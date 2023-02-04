function startMeeting (parentId, roomName) {
	const domain = 'meet.jit.si';
	const options = {
		roomName: roomName,
		width: "100%",
		height: "100%",
		parentNode: document.querySelector(`#${parentId}`),
		lang: 'en'
	};
	const api = new JitsiMeetExternalAPI(domain, options);
	api.executeCommand('overwriteConfig',
		{
			fileRecordingsEnabled: true,
			localRecording: {
			disable: false,
			notifyAllParticipants: true
			}
		}
	);
	api.addListener("videoConferenceLeft", () => { api.dispose(); startMeeting(parentId, roomName); });
}

async function startRecording(DotNetMeetingsComponent, selectedAudioInputId, saveInterval = 500) {
    let recordingManager = new RecordingManager(DotNetMeetingsComponent, saveInterval);
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