const STORAGE_KEY = "organizingVideosAppState";
const genres = ["映画", "スポーツ", "音楽", "学習", "旅行", "その他"];

const state = loadState();
let lastTransferId = null;

const views = {
  main: document.getElementById("mainMenu"),
  cases: document.getElementById("casesView"),
  history: document.getElementById("historyView"),
  signIn: document.getElementById("signInView"),
};

const welcomeLabel = document.getElementById("welcomeLabel");
const dropZone = document.getElementById("dropZone");
const fileInput = document.getElementById("fileInput");
const genreSelect = document.getElementById("genreSelect");
const transferDialog = document.getElementById("transferDialog");
const signOutDialog = document.getElementById("signOutDialog");

document.getElementById("myCasesButton").addEventListener("click", () => showView("cases"));
document.getElementById("recentButton").addEventListener("click", () => showView("history"));
document.getElementById("signInButton").addEventListener("click", () => showView("signIn"));
document.getElementById("signOutButton").addEventListener("click", () => signOutDialog.showModal());
document.getElementById("chooseFilesButton").addEventListener("click", () => fileInput.click());
document.getElementById("submitSignInButton").addEventListener("click", signIn);
document.getElementById("confirmSignOutButton").addEventListener("click", signOut);
document.getElementById("cancelTransferButton").addEventListener("click", cancelLastTransfer);

document.querySelectorAll("[data-back]").forEach((button) => {
  button.addEventListener("click", () => showView("main"));
});

dropZone.addEventListener("dragover", (event) => {
  event.preventDefault();
  dropZone.classList.add("drag-over");
});

dropZone.addEventListener("dragleave", () => {
  dropZone.classList.remove("drag-over");
});

dropZone.addEventListener("drop", (event) => {
  event.preventDefault();
  dropZone.classList.remove("drag-over");
  handleFiles(event.dataTransfer.files);
});

fileInput.addEventListener("change", () => {
  handleFiles(fileInput.files);
  fileInput.value = "";
});

document.getElementById("userNameInput").addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    signIn();
  }
});

render();

function loadState() {
  const emptyState = {
    currentUser: "",
    videos: [],
    history: [],
  };

  try {
    return { ...emptyState, ...JSON.parse(localStorage.getItem(STORAGE_KEY)) };
  } catch {
    return emptyState;
  }
}

function saveState() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

function showView(name) {
  Object.values(views).forEach((view) => view.classList.remove("active-view"));
  views[name].classList.add("active-view");
  render();
}

function handleFiles(fileList) {
  const files = Array.from(fileList).filter((file) => file.type.startsWith("video/") || isKnownVideo(file.name));
  if (files.length === 0) {
    return;
  }

  files.forEach((file) => addTransfer(file));
  showTransferDialog(state.videos.find((video) => video.id === lastTransferId));
}

function isKnownVideo(fileName) {
  return /\.(mp4|mov|avi|mkv|webm|wmv|m4v)$/i.test(fileName);
}

function addTransfer(file) {
  const now = new Date();
  const video = {
    id: crypto.randomUUID(),
    name: file.name,
    size: file.size,
    genre: genreSelect.value,
    transferredAt: now.toISOString(),
  };

  state.videos.unshift(video);
  state.history.unshift({
    ...video,
    action: "転送",
  });
  lastTransferId = video.id;
  saveState();
  render();
}

function showTransferDialog(video) {
  if (!video) return;

  document.getElementById("dialogFileName").textContent = video.name;
  document.getElementById("dialogFileSize").textContent = `${video.size.toLocaleString("ja-JP")} バイト`;
  document.getElementById("dialogGenre").textContent = video.genre;
  transferDialog.showModal();
}

function cancelLastTransfer() {
  if (!lastTransferId) return;

  const index = state.videos.findIndex((video) => video.id === lastTransferId);
  if (index >= 0) {
    const [removed] = state.videos.splice(index, 1);
    state.history.unshift({
      ...removed,
      action: "転送キャンセル",
      transferredAt: new Date().toISOString(),
    });
  }

  lastTransferId = null;
  saveState();
  render();
  transferDialog.close();
}

function signIn() {
  const input = document.getElementById("userNameInput");
  const name = input.value.trim();
  if (!name) {
    input.focus();
    return;
  }

  state.currentUser = name;
  saveState();
  input.value = "";
  showView("main");
}

function signOut() {
  state.currentUser = "";
  saveState();
  signOutDialog.close();
  showView("main");
}

function render() {
  const userName = state.currentUser || "ゲスト";
  welcomeLabel.textContent = `ようこそ、${userName}さん`;
  renderCases();
  renderHistory();
}

function renderCases() {
  const caseList = document.getElementById("caseList");
  caseList.innerHTML = "";

  genres.forEach((genre) => {
    const videos = state.videos.filter((video) => video.genre === genre);
    const card = document.createElement("article");
    card.className = "case-card";

    const title = document.createElement("h3");
    title.textContent = `${genre}ケース`;
    card.append(title);

    if (videos.length === 0) {
      const empty = document.createElement("p");
      empty.className = "meta";
      empty.textContent = "動画はまだありません。";
      card.append(empty);
    } else {
      const list = document.createElement("ul");
      list.className = "video-list";
      videos.forEach((video) => list.append(createVideoRow(video)));
      card.append(list);
    }

    caseList.append(card);
  });
}

function renderHistory() {
  const historyList = document.getElementById("historyList");
  historyList.innerHTML = "";

  if (state.history.length === 0) {
    const empty = document.createElement("p");
    empty.className = "empty-state";
    empty.textContent = "転送履歴はまだありません。";
    historyList.append(empty);
    return;
  }

  state.history.forEach((item) => {
    const row = document.createElement("article");
    row.className = "history-item";
    row.append(createVideoRow(item, item.action));
    historyList.append(row);
  });
}

function createVideoRow(video, action = "") {
  const row = document.createElement("li");
  row.className = "video-row";

  const name = document.createElement("strong");
  name.textContent = action ? `${action}: ${video.name}` : video.name;

  const meta = document.createElement("span");
  meta.className = "meta";
  meta.textContent = `${video.size.toLocaleString("ja-JP")} バイト / ${video.genre}ケース / ${formatDate(video.transferredAt)}`;

  row.append(name, meta);
  return row;
}

function formatDate(value) {
  return new Intl.DateTimeFormat("ja-JP", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
