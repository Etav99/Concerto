const FILE_EXTENSIONS = ["mp4", "webm", "ogg", "x-matroska"];

const CODECS = [
  "vp9",
  "vp9.0",
  "vp8",
  "vp8.0",
  "avc1",
  "av1",
  "h265",
  "h.265",
  "h264",
  "h.264",
  "opus"
];

// generate all mime types combinations from FILE_EXTENSIONS and CODECS in "video/fileExtension;codecs=codec" format
const MIME_TYPES = FILE_EXTENSIONS.reduce((mimeTypes, fileExtension) => {
    return mimeTypes.concat(CODECS.map(codec => `video/${fileExtension};codecs=${codec}`));
}, []);

class RecordingManager
{
    screenMediaStream;
    micMediaStream;
   
    audioContext;
    micInput;
    audioInput;
    audioOutput;

    outputStream;
    recorder;

    writeToFile;
    outputFileHandle;
    outputWriteable;
    outputBuffer
    outputFileExtension;

    saveInterval;

    dotNetMeetingsComponent

    videoFileWorker;

    constructor(dotNetMeetingsComponent, saveInterval = 500)
    {
        this.dotNetMeetingsComponent = dotNetMeetingsComponent;
        this.saveInterval = saveInterval;
    }
    
    getMimeTypeAndExtension()
    {
        for (const ext of FILE_EXTENSIONS) {
            for (const codec of CODECS) {
                var mimeType = `video/${ext};codecs=${codec}`;
                if (MediaRecorder.isTypeSupported(mimeType)) {
                    return [mimeType, ext];
                }
            }
        }
        return [null, null];
    };

    async startRecording(selectedAudioInputId)
    {
        window.recordingManager = this;
        this.audioContext = new AudioContext();


        try {
            this.screenMediaStream = await navigator.mediaDevices.getDisplayMedia({
                video: {
                    // @ts-ignore
                    displaySurface: 'browser',
                    frameRate: 30
                },
                audio: true,
                preferCurrentTab: true
            });

            this.audioOutput = this.audioContext.createMediaStreamDestination();
        }
        catch (e)
        {
            stopRecording();
            return;
        }
        
        // mix audio from screen and microphone
        if (selectedAudioInputId != "none") {
            if (selectedAudioInputId && selectedAudioInputId.length > 0)
                this.micMediaStream = await navigator.mediaDevices.getUserMedia({ audio: { deviceId: selectedAudioInputId } });
            else
                this.micMediaStream = await navigator.mediaDevices.getUserMedia({audio: true, video: false });
        }

        try {
            this.micInput = this.audioContext.createMediaStreamSource(this.micMediaStream);
            this.micInput.connect(this.audioOutput);
        }
        catch (e)
        {
            alert("Recording with no microphone!")
        }

        try {
            this.audioInput = this.audioContext.createMediaStreamSource(this.screenMediaStream);
            this.audioInput.connect(this.audioOutput);
        }
        catch (e)
        {
            alert("Recording with no audio!")
        }


        
        // mix video with audio
        this.outputStream = new MediaStream([...this.screenMediaStream.getVideoTracks(), ...this.audioOutput.stream.getAudioTracks()]);

        var [mimeType, extension] = this.getMimeTypeAndExtension();
        
        if (mimeType == null)
        {
            throw new Error("Recording is not supported");
        }

        this.outputFileExtension = extension;
        this.recorder = new MediaRecorder(this.outputStream, { mimeType: mimeType });
        
        this.videoFileWorker = new Worker("js/worker.js");
        this.videoFileWorker.onmessage = (e) => {
            let filename = window.prompt('Enter file name');
            let downloadLink = document.createElement('a');
            downloadLink.href = e.data;
            downloadLink.download = `${filename}.${recordingManager.outputFileExtension}`;
            document.body.appendChild(downloadLink);
            downloadLink.click();
            URL.revokeObjectURL(e.data);
            document.body.removeChild(downloadLink);
        };

        const recordingManager = this;
        this.recorder.ondataavailable = (e) =>
        {
            if (e.data.size > 0)
                recordingManager.videoFileWorker.postMessage(["d" , e.data]);
        };
        
        this.recorder.onstop = () =>
        {
            this.stream?.getTracks().forEach((track) => track.stop());
            recordingManager.screenMediaStream?.getTracks().forEach((track) => track.stop());
            recordingManager.micMediaStream?.getTracks().forEach((track) => track.stop());
            recordingManager.videoFileWorker.postMessage(["e"]);
        };

        // stop recording on stream inactive
        this.screenMediaStream?.getVideoTracks().forEach((track) => {
            track.onended = async () => {
                await recordingManager.stopRecording();
            };
        });
        
        this.micMediaStream?.getAudioTracks().forEach((track) => {
            track.onended = async () => {
                await recordingManager.stopRecording();
            };
        });
        
        
        this.videoFileWorker.postMessage(["b", this.mimeType, this.outputFileExtension]);
        this.recorder.start(this.saveInterval);
        await this.dotNetMeetingsComponent.invokeMethodAsync('RecordingStateChanged', true);
    }
    
    async stopRecording()
    {
        this.recorder.stop();
        await this.dotNetMeetingsComponent.invokeMethodAsync('RecordingStateChanged', false);
        window.recordingManager = null;
    }
}
