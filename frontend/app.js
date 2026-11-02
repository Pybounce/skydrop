const API_BASE = "https://api.skydrop.skybounce.io/File";

const fileId = decodeURIComponent(window.location.pathname.slice(1));
if (fileId) {
  document.getElementById("download-view").hidden = false;
  loadFile(fileId);
} else {
  document.getElementById("upload-view").hidden = false;
}

// Upload
const fileInput = document.getElementById("file-input");
const uploadStatus = document.getElementById("upload-status");

fileInput.addEventListener("change", () => {
  const file = fileInput.files[0];
  uploadFile(file);
});

function uploadFile(file) {
  if (!file) {
    return;
  }

  const xhr = new XMLHttpRequest();
  xhr.open("POST", API_BASE);
  xhr.setRequestHeader("Content-Type", "application/octet-stream");
  xhr.setRequestHeader("X-File-Name", encodeURIComponent(file.name));

  xhr.upload.addEventListener("progress", (event) => {
    if (event.lengthComputable) {
      const progress = Math.round((event.loaded / event.total) * 100);
      showUploadStatus(`Uploading ${file.name} [${progress}%]`);
    }
  });

  xhr.addEventListener("load", () => {
    if (xhr.status >= 200 && xhr.status < 300) {
      const fileId = xhr.responseText.trim();
      window.location.href = "/" + encodeURIComponent(fileId);
    } else if (xhr.status === 413) {
      showUploadError("That file is too large.");
    } else if (xhr.status === 429) {
      showUploadError(
        "The upload limit for Skydrop has been reached. Please try again later.",
      );
    } else {
      showUploadError(
        "Something went wrong uploading your file. Please try again.",
      );
    }
  });

  xhr.addEventListener("error", () => {
    showUploadError("The upload failed. Please try again.");
  });

  fileInput.disabled = true;
  showUploadStatus(`Uploading ${file.name} [0%]`);
  xhr.send(file);
}

function showUploadStatus(message) {
  uploadStatus.classList.remove("error");
  uploadStatus.textContent = message;
}

function showUploadError(message) {
  uploadStatus.classList.add("error");
  uploadStatus.textContent = message;
  fileInput.disabled = false;
  fileInput.value = "";
}

// Drag + Drop
const dropzone = document.getElementById("dropzone");
window.addEventListener("dragover", (event) => event.preventDefault());
window.addEventListener("drop", (event) => event.preventDefault());

dropzone.addEventListener("dragover", (event) => {
  event.preventDefault();
  event.dataTransfer.dropEffect = "copy";
  dropzone.classList.add("dragging");
});
dropzone.addEventListener("dragleave", (event) => {
  if (!dropzone.contains(event.relatedTarget)) {
    dropzone.classList.remove("dragging");
  }
});
dropzone.addEventListener("drop", (event) => {
  event.preventDefault();
  dropzone.classList.remove("dragging");
  if (fileInput.disabled) {
    return;
  }
  uploadFile(event.dataTransfer.files[0]);
});

// Download
const downloadStatus = document.getElementById("download-status");

async function loadFile(id) {
  let response;
  try {
    response = await fetch(`${API_BASE}/Metadata/${encodeURIComponent(id)}`);
  } catch {
    showDownloadError("Could not reach the server.");
    return;
  }

  if (response.status === 404) {
    showNotFound();
    return;
  }

  if (!response.ok) {
    showDownloadError("Something went wrong loading the file.");
    return;
  }

  const metadata = await response.json();
  showFile(id, metadata);
}

function showFile(id, metadata) {
  document.getElementById("file-name").textContent = metadata.name;
  document.getElementById("file-details").textContent =
    `${fileType(metadata.name)}`;
  document.getElementById("file-downloads").textContent =
    metadata.downloadCount;
  document.getElementById("file-size").textContent = formatBytes(metadata.size);

  document.getElementById("download-button").href =
    `${API_BASE}/Download/${encodeURIComponent(id)}`;
  document.getElementById("share-link").value = window.location.href;
  document.title = `${metadata.name} - Skydrop`;

  downloadStatus.textContent = "";
  document.getElementById("file-info").hidden = false;
  document.getElementById("file-stats").hidden = false;
  document.getElementById("download-button").hidden = false;
  document.getElementById("share").hidden = false;
}

function showNotFound() {
  document.getElementById("download-view").hidden = true;
  document.getElementById("not-found-view").hidden = false;
}

function showDownloadError(message) {
  downloadStatus.classList.add("error");
  downloadStatus.textContent = message;
}

const copyButton = document.getElementById("copy-button");
copyButton.addEventListener("click", async () => {
  try {
    await navigator.clipboard.writeText(
      document.getElementById("share-link").value,
    );
    copyButton.textContent = "Copied!";
  } catch {
    document.getElementById("share-link").select();
    copyButton.textContent = "Press Ctrl+C";
  }
  setTimeout(() => {
    copyButton.textContent = "Copy";
  }, 2000);
});

function formatBytes(bytes) {
  const units = ["bytes", "KB", "MB", "GB", "TB"];
  let value = bytes;
  let unit = 0;
  while (value >= 1000 && unit < units.length - 1) {
    value /= 1000;
    unit += 1;
  }
  const decimals = unit === 0 || value >= 100 ? 0 : 1;
  return `${value.toFixed(decimals)} ${units[unit]}`;
}

function fileType(fileName) {
  const dot = fileName.lastIndexOf(".");
  if (dot <= 0 || dot === fileName.length - 1) {
    return "File";
  }
  return `${fileName.slice(dot + 1).toUpperCase()} file`;
}
