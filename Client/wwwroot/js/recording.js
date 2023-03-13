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
    "h.264"
];

// 10GB
const MAX_RECORDING_SIZE = 10 * 1024 * 1024 * 1024;

async function startRecording(DotNetMeetingsComponent, selectedAudioInputId, filenameBase, saveInterval = 500) {
    let recordingManager = new RecordingManager(DotNetMeetingsComponent, filenameBase, saveInterval);
    await recordingManager.startRecording(selectedAudioInputId);
}

async function stopRecording() {
    if (window.recordingManager)
    {
        await window.recordingManager.stopRecording();
    }
}

async function getAudioInputs() {
    var AudioInputs = await navigator.mediaDevices.enumerateDevices().then((devices) => {
        return devices.filter((device) => device.kind === 'audioinput');
    });
    
    // convert to name uid pairs
    AudioInputsNames = [];
    AudioInputsIds = [];
    AudioInputs.forEach((device) => {
        AudioInputsNames.push(device.label);
        AudioInputsIds.push(device.deviceId);
    });

    return {Names: AudioInputsNames, Ids: AudioInputsIds}
}

class RecordingManager {
    screenMediaStream;
    micMediaStream;

    audioContext;
    micInput;
    audioInput;
    audioOutput;

    outputStream;
    recorder;

    saveInterval;

    dotNetMeetingsComponent

    videoFileWorker;

    filename
    mimeType
    extension
    recordingChunksLength
    recordingChunks

    constructor(dotNetMeetingsComponent, filenameBase, saveInterval = 500) {
        this.dotNetMeetingsComponent = dotNetMeetingsComponent;
        this.filenameBase = filenameBase;
        this.saveInterval = saveInterval;
    }

    getMimeTypeAndExtension() {
        for (const ext of FILE_EXTENSIONS) {
            for (const codec of CODECS) {
                var mimeType = `video/${ext};codecs=${codec},opus`;
                if (MediaRecorder.isTypeSupported(mimeType)) {
                    return [mimeType, ext];
                }
            }
        }
        return [null, null];
    };


    async startRecording(selectedAudioInputId) {
        this.part = 0;
        // generate filename from base and current date in DD-MM-YY_HH:mm:ss
        var filename = this.filenameBase + " " + new Date().toLocaleDateString().replace(/\//g, "-") + " " + new Date().toLocaleTimeString().replace(/:/g, "-");
        // remove characters not allowed in filename
        filename = filename.replace(/[\\/:*?"<>|]/g, "_");
        this.filename = filename;

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
        catch (e) {
            alert("Recording canceled or not supported!")
            this.stopRecording();
            return;
        }

        // mix audio from screen and microphone
        if (selectedAudioInputId != "none") {
            if (selectedAudioInputId && selectedAudioInputId.length > 0)
                this.micMediaStream = await navigator.mediaDevices.getUserMedia({ audio: { deviceId: selectedAudioInputId } });
            else
                this.micMediaStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
        }

        try {
            this.micInput = this.audioContext.createMediaStreamSource(this.micMediaStream);
            this.micInput.connect(this.audioOutput);
        }
        catch (e) {
            if (!confirm("Recording with no microphone!\nConfirm to start recording."))
            {
                this.stopRecording();
                return;
            }
        }

        try {
            this.audioInput = this.audioContext.createMediaStreamSource(this.screenMediaStream);
            this.audioInput.connect(this.audioOutput);
        }
        catch (e) {
            alert("Cannot get audio from browser tab or computer!\nScreen/tab was shared without audio or your browser doesn't support this feature. Recording will be stopped.");
            this.stopRecording();
            return;
        }

        // mix video with audio
        this.outputStream = new MediaStream([...this.screenMediaStream.getVideoTracks(), ...this.audioOutput.stream.getAudioTracks()]);

        [this.mimeType, this.extension] = this.getMimeTypeAndExtension();

        if (this.mimeType == null) {
            throw new Error("Recording is not supported");
        }

        this.recorder = new MediaRecorder(this.outputStream, { mimeType: this.mimeType });

        const recordingManager = this;

        this.recordingChunks = [];
        this.recordingChunksLength = 0;

        this.recorder.ondataavailable = (e) => {
            if (e.data.size > 0) {
                recordingManager.recordingChunks.push(e.data);
                recordingManager.recordingChunksLength += e.data.size;
            }
            if (recordingManager.recordingChunksLength > MAX_RECORDING_SIZE) {
                recordingManager.stopRecording();
            }
        };

        // stop tracks and save recording file on stop
        this.recorder.onstop = () => {
            recordingManager.saveRecording();
        };

        // stop recording on stream inactive
        this.screenMediaStream?.getVideoTracks().forEach((track) => {
            track.onended = async () => {
                await recordingManager.stopRecording();
            };
        });

        // stop recording on stream inactive
        this.micMediaStream?.getAudioTracks().forEach((track) => {
            track.onended = async () => {
                await recordingManager.stopRecording();
            };
        });

        this.recorder.start(this.saveInterval);
        enablePreventWindowClose("recording");
        await this.dotNetMeetingsComponent.invokeMethodAsync('RecordingStateChanged', true);
    }

    async stopRecording() {
        this.stopAllTracks();
        this.recorder?.stop();
        await this.dotNetMeetingsComponent.invokeMethodAsync('RecordingStateChanged', false);
        window.recordingManager = null;
    }

    stopAllTracks()
    {
        this.recorder?.stream?.getTracks().forEach((track) => track.stop());
        this.screenMediaStream?.getTracks().forEach((track) => track.stop());
        this.audioOutput?.stream?.getTracks().forEach((track) => track.stop());
    }

    saveRecording() {
         this.screenMediaStream?.getTracks().forEach((track) => track.stop());
         this.micMediaStream?.getTracks().forEach((track) => track.stop());
         this.saveRecordingFile(this.recordingChunks)
                .then(() => disablePreventWindowClose("recording"));
        this.recordingChunks = [];
    }

    async saveRecordingFile(recordingChunks) {
        let recording = new Blob([...recordingChunks], { type: this.mimeType });
        if (this.extension = "webm")
            recording = await fixWebmDuration(recording, { type: this.mimeType });

        let recordingUrl = URL.createObjectURL(recording);
        let downloadLink = document.createElement('a');
        downloadLink.href = recordingUrl;
        downloadLink.download = `${this.filename}.${this.extension}`;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        URL.revokeObjectURL(e.data);
        document.body.removeChild(downloadLink);
    }

}


