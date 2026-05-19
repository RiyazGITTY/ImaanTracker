const API_BASE = localStorage.getItem("imaan_api_base")
  || window.IMAAN_API_BASE
  || "http://localhost:5263/api";

const state = {
  token: localStorage.getItem("imaan_token") || "",
  userName: localStorage.getItem("imaan_user_name") || ""
};

const authPanel = document.querySelector("#authPanel");
const prayerPanel = document.querySelector("#prayerPanel");
const loginTab = document.querySelector("#loginTab");
const signupTab = document.querySelector("#signupTab");
const loginForm = document.querySelector("#loginForm");
const signupForm = document.querySelector("#signupForm");
const authMessage = document.querySelector("#authMessage");
const prayerMessage = document.querySelector("#prayerMessage");
const prayerList = document.querySelector("#prayerList");
const statusText = document.querySelector("#statusText");
const completeText = document.querySelector("#completeText");
const progressCircle = document.querySelector("#progressCircle");
const logoutButton = document.querySelector("#logoutButton");
const signupButton = document.querySelector("#signupButton");
const salaamText = document.querySelector("#salaamText");
const toast = document.querySelector("#toast");

document.querySelectorAll("[data-toggle-password]").forEach(button => {
  button.addEventListener("click", () => togglePassword(button));
});

loginTab.addEventListener("click", () => showAuthTab("login"));
signupTab.addEventListener("click", () => showAuthTab("signup"));
loginForm.addEventListener("submit", login);
signupForm.addEventListener("submit", signup);
logoutButton.addEventListener("click", logout);

if (state.token) {
  showPrayerPanel();
  loadToday();
}

function showAuthTab(tab) {
  const isLogin = tab === "login";
  loginTab.classList.toggle("active", isLogin);
  signupTab.classList.toggle("active", !isLogin);
  loginForm.classList.toggle("hidden", !isLogin);
  signupForm.classList.toggle("hidden", isLogin);
  setAuthMessage("");
}

async function signup(event) {
  event.preventDefault();
  setAuthMessage("");

  if (!signupForm.reportValidity()) return;

  const password = value("#signupPassword");
  const confirmPassword = value("#signupConfirmPassword");
  if (password !== confirmPassword) {
    setAuthMessage("Password and confirm password must match.");
    return;
  }

  const body = {
    fullName: value("#signupName"),
    email: value("#signupEmail"),
    password,
    mobileNumber: value("#signupMobile"),
    city: value("#signupCity"),
    country: value("#signupCountry"),
    latitude: 0,
    longitude: 0,
    calculationMethod: "Karachi",
    madhab: "Hanafi"
  };

  setSignupLoading(true);
  let response;
  try {
    response = await request("/Auth/register", { method: "POST", body });
    if (!response.ok) {
      setAuthMessage(await errorText(response) || "Could not create account.");
      return;
    }
  } finally {
    setSignupLoading(false);
  }

  signupForm.reset();
  showAuthTab("login");
  document.querySelector("#loginEmail").value = body.email;
  setAuthMessage("Successfully your account created.", true);
  showToast("Successfully your account created.");
}

async function login(event) {
  event.preventDefault();
  setAuthMessage("");

  const response = await request("/Auth/login", {
    method: "POST",
    body: {
      email: value("#loginEmail"),
      password: value("#loginPassword")
    }
  });

  if (!response.ok) {
    setAuthMessage("Invalid email or password.");
    return;
  }

  const data = await response.json();
  state.token = data.token;
  state.userName = data.fullName || value("#loginEmail");
  localStorage.setItem("imaan_token", state.token);
  localStorage.setItem("imaan_user_name", state.userName);
  showPrayerPanel();
  await loadToday();
}

async function loadToday() {
  setPrayerMessage("");
  const response = await request("/Prayer/today", { method: "GET", auth: true });

  if (!response.ok) {
    setPrayerMessage("Could not load today's prayers. Login again.");
    if (response.status === 401) logout();
    return;
  }

  renderToday(await response.json());
}

async function completePrayer(prayerName) {
  setPrayerMessage("");
  const response = await request("/Prayer/complete", {
    method: "POST",
    auth: true,
    body: { prayerName }
  });

  if (!response.ok) {
    setPrayerMessage("Could not save prayer. Try again.");
    return;
  }

  renderToday(await response.json());
}

function renderToday(today) {
  const percent = today.totalCount === 0
    ? 0
    : Math.round((today.completedCount / today.totalCount) * 100);

  statusText.textContent = `${today.completedCount} of ${today.totalCount} complete`;
  completeText.textContent = today.isComplete ? "All five prayers completed today." : "Keep going.";
  progressCircle.textContent = `${percent}%`;
  prayerList.replaceChildren();

  today.prayers.forEach(prayer => {
    const row = document.createElement("article");
    row.className = `prayer ${prayer.completed ? "completed" : ""}`;

    const info = document.createElement("div");
    const name = document.createElement("span");
    name.className = "prayer-name";
    name.textContent = prayer.prayerName;

    const status = document.createElement("span");
    status.className = "prayer-state";
    status.textContent = prayer.completed ? "Alhamdulillah" : "Pending";

    info.append(name, status);

    const button = document.createElement("button");
    button.type = "button";
    button.disabled = prayer.completed;
    button.textContent = prayer.completed ? "Completed" : "Mark complete";
    button.addEventListener("click", () => completePrayer(prayer.prayerName));

    row.append(info, button);
    prayerList.append(row);
  });
}

function showPrayerPanel() {
  authPanel.classList.add("hidden");
  prayerPanel.classList.remove("hidden");
  updateSalaam();
}

function logout() {
  state.token = "";
  state.userName = "";
  localStorage.removeItem("imaan_token");
  localStorage.removeItem("imaan_user_name");
  prayerPanel.classList.add("hidden");
  authPanel.classList.remove("hidden");
}

async function request(path, options) {
  const headers = {
    Accept: "application/json",
    ...(options.body ? { "Content-Type": "application/json" } : {}),
    ...(options.auth ? { Authorization: `Bearer ${state.token}` } : {})
  };

  return fetch(`${API_BASE}${path}`, {
    method: options.method,
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined
  });
}

async function errorText(response) {
  try {
    const data = await response.json();
    return Array.isArray(data) ? data.join(" ") : data?.message || "";
  } catch {
    return "";
  }
}

function value(selector) {
  return document.querySelector(selector).value.trim();
}

function setAuthMessage(text, ok = false) {
  authMessage.textContent = text;
  authMessage.classList.toggle("ok", ok);
}

function setPrayerMessage(text) {
  prayerMessage.textContent = text;
}

function togglePassword(button) {
  const input = document.querySelector(button.dataset.togglePassword);
  const isPassword = input.type === "password";
  input.type = isPassword ? "text" : "password";
  button.classList.toggle("active", isPassword);
  button.setAttribute("aria-label", isPassword ? "Hide password" : "Show password");
}

function setSignupLoading(isLoading) {
  signupButton.disabled = isLoading;
  signupButton.classList.toggle("loading", isLoading);
}

function showToast(text) {
  toast.textContent = text;
  toast.classList.add("show");
  window.clearTimeout(showToast.timeout);
  showToast.timeout = window.setTimeout(() => toast.classList.remove("show"), 3200);
}

function updateSalaam() {
  const name = state.userName || readJwtName(state.token) || "Friend";
  salaamText.textContent = `Assalamu Alaikum ${name},`;
}

function readJwtName(token) {
  try {
    const encoded = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = encoded.padEnd(encoded.length + (4 - encoded.length % 4) % 4, "=");
    const payload = JSON.parse(atob(padded));
    return payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || payload.name || "";
  } catch {
    return "";
  }
}
