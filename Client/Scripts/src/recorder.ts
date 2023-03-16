import { output } from "../webpack.config";
import fixWebmDuration from 'webm-duration-fix';

declare global {
    interface Window {
        recordingManager: RecordingManager | null | undefined;
        preventWindowCloseActiveRequests: Set<string>;
        enablePreventWindowClose: (requestId: string) => void;
        disablePreventWindowClose: (requestId: string) => void;
        initializeRecordingManager: (canvas: HTMLCanvasElement) => void;
    }
}

type DotNet = any;
type DotNetObject = any;

const FILE_EXTENSIONS = ["mp4", "webm", "ogg", "x-matroska"];
const CODECS = ["vp9", "vp9.0", "vp8", "vp8.0", "avc1", "av1", "h265", "h.265", "h264", "h.264"];
const MAX_RECORDING_SIZE = 10 * 1024 * 1024 * 1024;

export async function initializeRecordingManager(videoPreviewParentId?: string, dotNetCaller?: DotNetObject): Promise<RecordingManager> {
    if (window.recordingManager != undefined && window.recordingManager != null) {
        throw new Error("RecordingManager already initialized");
    }

    let recordingManager = new RecordingManager(dotNetCaller);
    if (videoPreviewParentId) {
        recordingManager.createPreview(videoPreviewParentId);
    }
    window.recordingManager = recordingManager;
    return recordingManager;
}

function getMimeTypeAndExtension(): [string, string]   {
    for (const ext of FILE_EXTENSIONS) {
        for (const codec of CODECS) {
            var mimeType = `video/${ext};codecs=${codec},opus`;
            if (MediaRecorder.isTypeSupported(mimeType)) {
                return [mimeType, ext];
            }
        }
    }
    throw new Error("Browser doesn't support any of the codecs");
}

function getTimeStamp(): string {
    return new Date().toLocaleDateString().replace(/\//g, "-") + " " + new Date().toLocaleTimeString().replace(/:/g, "-");
}

function cleanFilename(filename: string): string {
    return filename.replace(/[\\/:*?"<>|]/g, "_");
}

export async function getVideoInputs(): Promise<{ Names: string[], Ids: string[] }> {

    await navigator.mediaDevices.getUserMedia({video: true}).catch((e) => { console.log(e); });
    return await getMediaInputs("videoinput");
}

export async function getAudioInputs(): Promise<{ Names: string[], Ids: string[] }> {
    await navigator.mediaDevices.getUserMedia({audio: true}).catch((e) => { console.log(e); });
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
    
    previewCanvas?: HTMLCanvasElement | null;
    previewCanvasContext?: CanvasRenderingContext2D | null;

    canvas: HTMLCanvasElement;
    canvasContext: CanvasRenderingContext2D;
    canvasStream: MediaStream | null = null;

    audioContext: AudioContext = new AudioContext();
    microphoneInput: MediaStreamAudioSourceNode | null = null;
    audioOutput: MediaStreamAudioDestinationNode | null = null;

    outputStream: MediaStream | null = null;
    recorder: MediaRecorder | null = null;

    readonly saveInterval = 500;

    recordingStore: RecordingStore | null = null; 

    canRecord = () => { return this.webcam.isActive() && this.microphone.isActive() };

    public constructor(dotnetCaller: DotNetObject) {
        this.dotnetCaller = dotnetCaller;

        var mimeType: string, extension: string;
        [mimeType, extension] = getMimeTypeAndExtension();
        this.recordingStore = new RecordingStore(mimeType, extension);

        this.canvas = document.createElement("canvas");
        this.canvasContext = this.canvas.getContext("2d");

        this.canvasStream = this.canvas.captureStream(30);

        this.audioOutput = this.audioContext.createMediaStreamDestination();
        this.drawCanvasFrame = this.drawCanvasFrame.bind(this);
        this.drawCanvasFrame();
    }

    private drawCanvasFrame() {
        requestAnimationFrame(this.drawCanvasFrame);
        if(this.webcam.isActive())
        {
            this.canvasContext.drawImage(this.webcam.getPreview(), 0, 0, this.canvas.width, this.canvas.height);
            this.previewCanvasContext?.drawImage(this.canvas, 0, 0, this.previewCanvas.width, this.previewCanvas.height);
        }
        else
        {
            this.canvasContext.fillStyle = "black";
            this.canvasContext.fillRect(0, 0, this.canvas.width, this.canvas.height);
            if(this.previewCanvas)
            {
                this.previewCanvas.width = 1280;
                this.previewCanvas.height = 720;
                this.previewCanvasContext.fillStyle = "black";
                this.previewCanvasContext.fillRect(0, 0, this.previewCanvas.width, this.previewCanvas.height);
                this.previewCanvasContext.fillStyle = "white";
                this.previewCanvasContext.font = "90px Calibri";
                this.previewCanvasContext.textAlign = "center";
                this.previewCanvasContext.fillText("No camera input", this.previewCanvas.width / 2, this.previewCanvas.height / 2);
            }
        }
    }

    public createPreview(parentId: string) {
        if(this.previewCanvas)
            throw new Error("Preview already exists.");

        let previewCanvas = document.createElement("canvas");
        this.previewCanvas = previewCanvas;
        this.previewCanvasContext = this.previewCanvas.getContext("2d");
        this.previewCanvas.width = 1280;
        this.previewCanvas.height = 720;

        if(parentId)
        {
            let videoPreviewParent = document.getElementById(parentId);
            console.log(parentId)
            videoPreviewParent.appendChild(previewCanvas);
        }
    }

    public removePreview() {
        if(!this.previewCanvas)
            return;
        this.previewCanvas.remove();
        this.previewCanvas = null;
        this.previewCanvasContext = null;
    }

    public async setWebcam(deviceId: string) {
        await this.webcam.start(deviceId);
        this.canvas.width = this.webcam.getWidth() as number;
        this.canvas.height = this.webcam.getHeight() as number;
        this.previewCanvas.width = this.canvas.width;
        this.previewCanvas.height = this.canvas.height;
    }

    public async setMicrophone(deviceId: string) {
        if(this.microphoneInput)
        {
            this.microphoneInput.disconnect();
            this.microphoneInput = null;
        }

        await this.microphone.start(deviceId);

        if(!this.microphone.active)
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

        this.recorder.onstop = () => {
            recordingManager.finalizeRecording();
        };

        // stop recording on stream inactive
        // this.screenMediaStream?.getTracks().forEach((track) => {
        //     track.onended = async () => {
        //         await recordingManager.stopRecording();
        //     };
        // });

        this.recorder.start(this.saveInterval);
        window.enablePreventWindowClose("recording");
        // @ts-ignore
        await this.dotnetCaller.invokeMethodAsync("RecordingStateChanged", true);
    }

    stopRecording() {
        if(this.recorder?.state != "inactive")
            this.recorder?.stop();
    }

    private async finalizeRecording() {
        this.recorder = null;
        this.recordingStore.save();
        window.disablePreventWindowClose("recording");
        this.recordingStore.clear();
        // @ts-ignore
        await this.dotnetCaller.invokeMethodAsync("RecordingStateChanged", false);
    }

    public async dispose()
    {
        await this.stopRecording();
        window.recordingManager.removePreview();
        if(window.recordingManager == this)
            window.recordingManager = null;
    }

}

class RecordingStore {
    chunks: Blob[];
    length: number;
    mimeType: string;
    extension: string;

    public constructor(mimeType: string, extension: string) {
        this.chunks = [];
        this.length = 0;
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

    public async save(filename: string | null = null) {
        let recording = new Blob([...this.chunks], { type: this.mimeType });
        if ((this.extension = "webm"))
            recording = await fixWebmDuration(recording);


        if(filename == null)
        {
            var filename = prompt("Please enter a filename", "recording");
            if (filename == null || filename == "") {
                filename = "recording";
            }
        }

        filename = `${filename}_${getTimeStamp()}.${this.extension}`;
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
        this.preview.srcObject = null;
        this.deviceId = null;
        this.width = this.height = null;
        this.active = false;
    }
}

class Microphone
{
    active: boolean = false;
    deviceId: string | null = null;

    stream: MediaStream | null = null;

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