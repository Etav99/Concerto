function downloadFile (fileName, url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}

function startMeeting () {
		const domain = 'meet.jit.si';
		const options = {
			roomName: 'JitsiMeetAPIExample',
			width: 700,
			height: 700,
			parentNode: document.querySelector('#meet'),
			lang: 'en'
		};
		const api = new JitsiMeetExternalAPI(domain, options);
}