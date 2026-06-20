// Image interop — load files from disk as base64 data URIs
window.tldrawImageInterop = {
    loadImageFile: function () {
        return new Promise((resolve, reject) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = 'image/*';
            input.onchange = () => {
                const file = input.files[0];
                if (!file) { resolve(null); return; }
                const reader = new FileReader();
                reader.onload = () => resolve({
                    name: file.name,
                    dataUri: reader.result,
                    width: 0,
                    height: 0
                });
                reader.onerror = () => reject(reader.error);
                reader.readAsDataURL(file);
            };
            input.click();
        });
    },

    getImageDimensions: function (dataUri) {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => resolve({ width: img.naturalWidth, height: img.naturalHeight });
            img.onerror = () => resolve({ width: 200, height: 200 });
            img.src = dataUri;
        });
    }
};
