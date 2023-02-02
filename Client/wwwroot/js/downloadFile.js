import '@jitsi/external_api';

function downloadFile (fileName, url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

/*async function startRecording(meetingInstance, saveInterval = 500) {
    let stream = await navigator.mediaDevices.getDisplayMedia({
        // @ts-ignore
        video: {
            displaySurface: 'browser',
            frameRate: 30
        },
        audio: true,
        preferCurrentTab: true
    });

    const mediaRecorder = new MediaRecorder(stream);
    let recordedChunks = [];

    mediaRecorder.ondataavailable = async function (e) {
        if (e.data.size > 0) {
            recordedChunks.push(e.data);
                // e.data.arrayBuffer().then(
                // async buffer => await meetingInstance.invokeMethodAsync("ProcessRecordingChunk", new Uint8Array(buffer))
        }
            //await meetingInstance.invokeMethodAsync("ProcessRecordingChunk", e.data.arrayBuffer());
    }

    mediaRecorder.onstop = function () {
        const blob = new Blob(recordedChunks, {
            type: 'video/webm'
        });

        let filename = window.prompt('Enter file name');
        let downloadLink = document.createElement('a');

        downloadLink.href = URL.createObjectURL(blob);
        downloadLink.download = `＄{filename}.webm`;

        document.body.appendChild(downloadLink);
        downloadLink.click();
        URL.revokeObjectURL(blob);
        document.body.removeChild(downloadLink);
    };

    mediaRecorder.start(saveInterval);
    return DotNet.createJSObjectReference(mediaRecorder);
}*/

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