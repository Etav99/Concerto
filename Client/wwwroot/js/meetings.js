function startMeeting (parentId, domain, roomName, token, caller) {
	const options = {
		jwt: token,
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

    api.addListener("videoConferenceLeft", async () => {
		await caller.invokeMethodAsync("OnVideoConferenceLeft");
    });

	return api;
}

function removeChildNodes(parentId) {
    const parent = document.getElementById(parentId);
    while (parent.firstChild) {
        parent.removeChild(parent.firstChild);
    }
}