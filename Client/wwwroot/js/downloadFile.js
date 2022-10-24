function downloadFile (fileName, url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

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
			fileRecordingsEnabled: true
		}
	);
	api.addListener("videoConferenceLeft", () => { api.dispose(); startMeeting(parentId, roomName) })
}