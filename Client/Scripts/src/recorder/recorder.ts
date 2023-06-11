import { url } from 'inspector';
import fixWebmDuration from 'webm-duration-fix';
import { getAnySupportedVideoMimeTypeAndExtension } from '../utilities/codecCheck';
declare const DotNet: typeof import("@microsoft/dotnet-js-interop").DotNet;
interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

declare global {
    interface Window {
        recordingManager: RecordingManager | null | undefined;
        preventWindowCloseActiveRequests: Set<string>;
        enablePreventWindowClose: (requestId: string) => void;
        disablePreventWindowClose: (requestId: string) => void;
        initializeRecordingManager: (canvas: HTMLCanvasElement) => void;
    }
}

const MAX_RECORDING_SIZE = 10 * 1024 * 1024 * 1024;

export async function initializeRecordingManager(videoPreviewParentId?: string, dotNetCaller?: DotNetObject): Promise<RecordingManager> {
    if (window.recordingManager != undefined && window.recordingManager != null) {
        throw new Error("RecordingManager already initialized");
    }

    let recordingManager = new RecordingManager(dotNetCaller);
    if (videoPreviewParentId) {
        recordingManager.appendPreviews(videoPreviewParentId);
    }
    window.recordingManager = recordingManager;
    return recordingManager;
}

function getTimeStamp(): string {
    return new Date().toLocaleDateString().replace(/\//g, "-") + " " + new Date().toLocaleTimeString().replace(/:/g, "-");
}

function cleanFilename(filename: string): string {
    return filename.replace(/[\\/:*?"<>|]/g, "_");
}

export async function getVideoInputs(): Promise<{ Names: string[], Ids: string[] }> {
    try{
        var stream = await navigator.mediaDevices.getUserMedia({video: true});
        stream?.getTracks().forEach((track) => track.stop());
    }
    catch(e) {
        console.log(e);
    }
    return await getMediaInputs("videoinput");
}

export async function getAudioInputs(): Promise<{ Names: string[], Ids: string[] }> {
    try{
        var stream = await navigator.mediaDevices.getUserMedia({audio: true});
        stream?.getTracks().forEach((track) => track.stop());
    }
    catch(e) {
        console.log(e);
    }
    return await getMediaInputs("audioinput");
}

async function getMediaInputs(kind: string) {
    var MediaInputs = await navigator.mediaDevices.enumerateDevices().then((devices) => {
        return devices.filter((device) => device.kind === kind);
    });

    // convert to name uid pairs
    var MediaInputsNames: string[] = [];
    var MediaInputsIds: string[] = [];
    MediaInputs.forEach((device) => {
        MediaInputsNames.push(device.label);
        MediaInputsIds.push(device.deviceId);
    });

    return { Names: MediaInputsNames, Ids: MediaInputsIds };
}

export class RecordingManager {
    dotnetCaller: DotNetObject;

    webcam: Webcam = new Webcam();
    microphone: Microphone = new Microphone();
    
    videoPreview: HTMLVideoElement = document.createElement("video");

    canvas: HTMLCanvasElement = document.createElement("canvas");
    canvasContext: CanvasRenderingContext2D;
    canvasStream: MediaStream;

    audioContext: AudioContext = new AudioContext();
    microphoneInput: MediaStreamAudioSourceNode | null = null;
    audioOutput: MediaStreamAudioDestinationNode | null = null;

    outputStream: MediaStream | null = null;
    recorder: MediaRecorder | null = null;

    readonly saveInterval = 500;

    recordingStore: RecordingStore; 
    recordingPreview: HTMLVideoElement = document.createElement("video");

    canRecord = () => { return this.webcam.isActive() && this.microphone.isActive() };

    public constructor(dotnetCaller: DotNetObject) {
        this.dotnetCaller = dotnetCaller;

        this.canvasContext = this.canvas.getContext("2d");
        this.canvasStream = this.canvas.captureStream(30);

        this.videoPreview.controls = false;
        this.videoPreview.muted = true;
        this.videoPreview.autoplay = true;
        this.videoPreview.style.pointerEvents = "none";
        this.videoPreview.srcObject = this.canvasStream;
        this.videoPreview.classList.add("preview");

        this.recordingPreview.controls = true;
        this.recordingPreview.style.display = "none";
        this.recordingPreview.classList.add("preview");

        var mimeType: string, extension: string;
        [mimeType, extension] = getAnySupportedVideoMimeTypeAndExtension();
        this.recordingStore = new RecordingStore(mimeType, extension);

        this.audioOutput = this.audioContext.createMediaStreamDestination();
        this.drawCanvasFrame = this.drawCanvasFrame.bind(this);
        this.drawCanvasFrame();
    }

    private drawCanvasFrame() {
        requestAnimationFrame(this.drawCanvasFrame);
        if(this.webcam.isActive())
        {
            this.canvasContext.drawImage(this.webcam.getPreview(), 0, 0, this.canvas.width, this.canvas.height);
        }
        else
        {
            this.canvasContext.fillStyle = "black";
            this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);
            if(this.canvasContext)
            {
                this.canvasContext.fillStyle = "black";
                this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);
                this.canvasContext.fillStyle = "white";
                this.canvasContext.font = "12px Calibri";
                this.canvasContext.textAlign = "center";
                this.canvasContext.fillText("No camera input", this.canvas.width / 2, this.canvas.height / 2);
            }
        }
    }

    public appendPreviews(parentId: string) {
            let videoPreviewParent = document.getElementById(parentId);
            videoPreviewParent.appendChild(this.videoPreview);
            videoPreviewParent.appendChild(this.recordingPreview);
    }

    public removePreview() {
        this.videoPreview?.remove();
        this.recordingPreview?.remove();
    }

    public async setWebcam(deviceId: string) {
        await this.webcam.start(deviceId);
        if(!this.webcam.isActive())
            return;

        this.canvas.width = this.webcam.getWidth() as number;
        this.canvas.height = this.webcam.getHeight() as number;
    }

    public async setMicrophone(deviceId: string) {
        if(this.microphoneInput)
        {
            this.microphoneInput.disconnect();
            this.microphoneInput = null;
        }

        await this.microphone.start(deviceId);
        if(!this.microphone.isActive())
            return;

        this.microphoneInput = this.audioContext.createMediaStreamSource(this.microphone.getStream());
        this.microphoneInput.connect(this.audioOutput);
    }

    public async startRecording() {
        if(!this.canRecord())
        {
            alert("Please select a webcam and microphone.");
            return;
        }

        this.audioContext.resume();
        this.outputStream = new MediaStream([...this.canvasStream.getVideoTracks(), ...this.audioOutput.stream.getAudioTracks()]);
        this.recorder = new MediaRecorder(this.outputStream, { mimeType: this.recordingStore.getMimeType() });

        const recordingManager = this;

        this.recorder.ondataavailable = (e) => {
            recordingManager.recordingStore.addChunk(e.data);

            if (recordingManager.recordingStore.maxSizeReached())
                recordingManager.stopRecording();
        };

        this.recorder.onstop = () => recordingManager.finishRecording();

        // stop recording on stream inactive
        // this.screenMediaStream?.getTracks().forEach((track) => {
        //     track.onended = async () => {
        //         await recordingManager.stopRecording();
        //     };
        // });

        this.recorder.start(this.saveInterval);
        window.enablePreventWindowClose("recording");
        await this.dotnetCaller.invokeMethodAsync("RecordingStateChanged", true);
    }

    stopRecording() {
        if(this.recorder?.state != "inactive")
            this.recorder?.stop();
    }

    private async finishRecording() {
        this.recorder = null;
        await this.recordingStore.generateOutputAsync();
        this.recordingStore.clear();

        let output = this.recordingStore.getOutput();

        // await this.dotnetCaller.invokeMethodAsync("RecordingStateChanged", false);
        await this.dotnetCaller.invokeMethodAsync("RecordingFinished", DotNet.createJSStreamReference(output), this.recordingStore.extension);

        this.videoPreview.style.display = "none";
        this.recordingPreview.style.display = "initial";
        this.recordingPreview.controls = true;
        this.recordingPreview.src = URL.createObjectURL(output);
        this.recordingPreview.load();
    }

    public finalizeRecording() {
        this.videoPreview.style.display = "initial";
        this.recordingPreview.style.display = "none";
        URL.revokeObjectURL(this.recordingPreview.src);
        this.recordingPreview.src = "";
        this.recordingPreview.load();

        window.disablePreventWindowClose("recording");
    }

    public async saveLocally(filename: string | null = null) {
        await this.recordingStore.saveOutputAsync(filename);
    }

    public async dispose()
    {
        await this.stopRecording();
        this.webcam.stop();
        this.microphone.stop();
        window.recordingManager.removePreview();
        if(window.recordingManager == this)
            window.recordingManager = null;
    }

}

class RecordingStore {
    chunks: Blob[] = [];
    length: number = 0;
    mimeType: string;
    extension: string;
    output: Blob | null = null;

    public constructor(mimeType: string, extension: string) {
        this.mimeType = mimeType;
        this.extension = extension;
    }

    public getMimeType = () => this.mimeType;

    public addChunk(chunk: Blob) {
        if(chunk.size == 0) return;
        this.chunks.push(chunk);
        this.length += chunk.size;
    }

    public clear() {
        this.chunks = [];
        this.length = 0;
    }

    public maxSizeReached(): boolean {
        return this.length >= MAX_RECORDING_SIZE;
    }

    public async generateOutputAsync() {
        let recording = new Blob([...this.chunks], { type: this.mimeType });
        if ((this.extension = "webm"))
            recording = await fixWebmDuration(recording);
        
        this.output = recording;
    }

    public getOutput(): Blob {
        if(this.output == null) throw new Error("No output to return.");
        return this.output;
    }

    public clearOutput() {
        this.output = null;
    }

    public async saveOutputAsync(filename: string | null = null) {
        if(this.output == null) throw new Error("No output to save.");
        let recording = this.output;
        if(filename == null)
        {
            var filename = prompt("Please enter a filename", "recording");
            if (filename == null || filename == "") {
                filename = "recording";
            }
        }

        // filename = `${filename}_${getTimeStamp()}.${this.extension}`;
        filename = `${filename}.${this.extension}`;
        filename = cleanFilename(filename);

        let recordingUrl = URL.createObjectURL(recording);
        let downloadLink = document.createElement("a");
        downloadLink.href = recordingUrl;
        downloadLink.download = filename;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        URL.revokeObjectURL(recordingUrl);
        document.body.removeChild(downloadLink);
    }
}

class Webcam
{
    private active: boolean = false;
    private deviceId: string | null = null;
    private width: number | null = null;
    private height: number | null = null;

    private preview: HTMLVideoElement;
    private stream: MediaStream | null = null;

    public constructor() {
        this.preview = document.createElement("video");
        this.preview.muted = true;
        this.preview.autoplay = true;
    }

    public isActive = () => this.active;
    public getPreview = () => this.preview;
    public getStream = () => this.stream;
    public getWidth = () => this.width;
    public getHeight = () => this.height;

    public async start(deviceId: string) {
        if(deviceId == this.deviceId)
            return;

        if(this.active)
            this.stop();

        if(deviceId == null || deviceId == undefined || deviceId == "" || deviceId == "none")
            return;

        this.deviceId = deviceId;

        try
        {
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: { deviceId: deviceId }
            });
        }
        catch(e)
        {
            alert("Failed to start camera.");
            console.log(e);
            this.stop();
            return;
        }

        const videoTrack = this.stream.getVideoTracks()[0];
        const settings = videoTrack.getSettings();
        this.width = settings.width;
        this.height = settings.height;

        this.preview.srcObject = this.stream;
        this.preview.play();
        this.active = true;
    }

    public stop() {
        this.stream?.getTracks().forEach((track) => track.stop());
        this.preview.remove();
        this.deviceId = null;
        this.width = this.height = null;
        this.active = false;
    }
}

class Microphone
{
    private active: boolean = false;
    private deviceId: string | null = null;
    private stream: MediaStream | null = null;

    public constructor() { }

    public isActive(): boolean { return this.active; }
    public getStream(): MediaStream | null { return this.stream; }

    public async start(deviceId: string) {
        if(deviceId == this.deviceId)
            return;

        if(this.active)
            this.stop();

        if(deviceId == null || deviceId == undefined || deviceId == "" || deviceId == "none")
            return;

        this.deviceId = deviceId;

        try
        {
            this.stream = await navigator.mediaDevices.getUserMedia({
                audio: { deviceId: deviceId }
            });
        }
        catch(e)
        {
            alert("Failed to start microphone.");
            console.log(e);
            this.stop();
            return;
        }

        this.active = true;
    }

    public stop() {
        this.stream?.getTracks().forEach((track) => track.stop());
        this.deviceId = null;
        this.active = false;
    }
}