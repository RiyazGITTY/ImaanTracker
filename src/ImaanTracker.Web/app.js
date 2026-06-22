const API_BASE = localStorage.getItem("imaan_api_base")
  || window.IMAAN_API_BASE
  || "http://localhost:5263/api";

const state = {
  token: localStorage.getItem("imaan_token") || "",
  userName: localStorage.getItem("imaan_user_name") || "",
  userId: localStorage.getItem("imaan_user_id") || "",
  isAdmin: localStorage.getItem("imaan_is_admin") === "true"
};

const authPanel = document.querySelector("#authPanel");
const prayerPanel = document.querySelector("#prayerPanel");
const adminPanel = document.querySelector("#adminPanel");
const loginTab = document.querySelector("#loginTab");
const signupTab = document.querySelector("#signupTab");
const loginForm = document.querySelector("#loginForm");
const signupForm = document.querySelector("#signupForm");
const authMessage = document.querySelector("#authMessage");
const prayerMessage = document.querySelector("#prayerMessage");
const adminMessage = document.querySelector("#adminMessage");
const prayerList = document.querySelector("#prayerList");
const prayerStats = document.querySelector("#prayerStats");
const periodTabs = document.querySelectorAll("[data-period]");
const previousMonthButton = document.querySelector("#previousMonthButton");
const nextMonthButton = document.querySelector("#nextMonthButton");
const calendarTitle = document.querySelector("#calendarTitle");
const calendarGrid = document.querySelector("#calendarGrid");
const adminSummary = document.querySelector("#adminSummary");
const adminUsers = document.querySelector("#adminUsers");
const statusText = document.querySelector("#statusText");
const completeText = document.querySelector("#completeText");
const progressCircle = document.querySelector("#progressCircle");
const logoutButton = document.querySelector("#logoutButton");
const adminLogoutButton = document.querySelector("#adminLogoutButton");
const adminButton = document.querySelector("#adminButton");
const backToPrayersButton = document.querySelector("#backToPrayersButton");
const signupButton = document.querySelector("#signupButton");
const loginButton = document.querySelector("#loginButton") || loginForm.querySelector("button[type='submit']");
const salaamText = document.querySelector("#salaamText");
const toast = document.querySelector("#toast");
const dateCard = document.querySelector("#dateCard");
const dateCardValue = document.querySelector("#dateCardValue");

let showingHijriDate = false;
let selectedDate = toDateKey(new Date());
let selectedPeriod = "day";
let calendarDate = new Date();

document.querySelectorAll("[data-toggle-password]").forEach(button => {
  button.addEventListener("click", () => togglePassword(button));
});

loginTab.addEventListener("click", () => showAuthTab("login"));
signupTab.addEventListener("click", () => showAuthTab("signup"));
loginForm.addEventListener("submit", login);
signupForm.addEventListener("submit", signup);
logoutButton.addEventListener("click", logout);
adminLogoutButton.addEventListener("click", logout);
adminButton.addEventListener("click", showAdminPanel);
backToPrayersButton.addEventListener("click", async () => {
  showPrayerPanel();
  await loadPrayerDashboard();
});
periodTabs.forEach(button => {
  button.addEventListener("click", () => setStatsPeriod(button.dataset.period));
});
previousMonthButton.addEventListener("click", () => changeCalendarMonth(-1));
nextMonthButton.addEventListener("click", () => changeCalendarMonth(1));
dateCard.addEventListener("click", toggleDateCard);
renderDateCard();

if (state.token) {
  hydrateAuthFromToken();
  if (state.isAdmin) {
    showAdminPanel();
  } else {
    showPrayerPanel();
    loadPrayerDashboard();
  }
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
  setLoginLoading(true);

  let response;
  try {
    response = await request("/Auth/login", {
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
    state.userId = data.userId || "";
    state.userName = data.fullName || value("#loginEmail");
    state.isAdmin = Boolean(data.isAdmin || data.roles?.includes("Admin"));
    localStorage.setItem("imaan_token", state.token);
    localStorage.setItem("imaan_user_id", state.userId);
    localStorage.setItem("imaan_user_name", state.userName);
    localStorage.setItem("imaan_is_admin", String(state.isAdmin));

    if (state.isAdmin) {
      showAdminPanel();
    } else {
      showPrayerPanel();
      await loadPrayerDashboard();
    }
  } finally {
    setLoginLoading(false);
  }
}

async function loadToday() {
  selectedDate = toDateKey(new Date());
  calendarDate = new Date();
  await loadPrayerDashboard();
}

async function loadPrayerDashboard() {
  await Promise.all([
    loadPrayerDate(selectedDate),
    loadPrayerStats(),
    loadCalendar()
  ]);
}

async function loadPrayerDate(dateKey) {
  setPrayerMessage("");
  const todayKey = toDateKey(new Date());
  const path = dateKey === todayKey
    ? "/Prayer/today"
    : `/Prayer/date?date=${encodeURIComponent(dateKey)}`;
  const response = await request(path, { method: "GET", auth: true });

  if (!response.ok) {
    setPrayerMessage("Could not load prayers. Login again.");
    if (response.status === 401) logout();
    return;
  }

  renderToday(await response.json());
}

async function loadPrayerStats() {
  const response = await request(`/Prayer/stats?period=${selectedPeriod}&date=${encodeURIComponent(selectedDate)}`, {
    method: "GET",
    auth: true
  });

  if (!response.ok) {
    setPrayerMessage("Could not load prayer summary.");
    return;
  }

  renderPrayerStats(await response.json());
}

async function loadCalendar() {
  const response = await request(`/Prayer/calendar?year=${calendarDate.getFullYear()}&month=${calendarDate.getMonth() + 1}`, {
    method: "GET",
    auth: true
  });

  if (!response.ok) {
    setPrayerMessage("Could not load calendar.");
    return;
  }

  renderCalendar(await response.json());
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

  const today = await response.json();
  renderToday(today);
  await Promise.all([loadPrayerStats(), loadCalendar()]);
}

function renderToday(today) {
  const percent = today.totalCount === 0
    ? 0
    : Math.round((today.completedCount / today.totalCount) * 100);

  selectedDate = toDateKey(new Date(today.logDate));
  const todayKey = toDateKey(new Date());
  const isToday = selectedDate === todayKey;

  statusText.textContent = `${today.completedCount} prayed, ${today.missedCount || 0} missed`;
  completeText.textContent = selectedDate === todayKey
    ? (today.isComplete ? "All five prayers completed today." : "Keep going.")
    : formatDisplayDate(new Date(today.logDate));
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
    status.textContent = prayer.completed ? "Prayed" : prayer.status;

    info.append(name, status);

    const button = document.createElement("button");
    button.type = "button";
    
    // Check if date is today or yesterday
    const currentDate = new Date();
    const yesterday = new Date(currentDate);
    yesterday.setDate(yesterday.getDate() - 1);
    const todayKey = toDateKey(currentDate);
    const yesterdayKey = toDateKey(yesterday);
    const isAllowedDate = selectedDate === todayKey || selectedDate === yesterdayKey;
    
    button.disabled = prayer.completed || !isAllowedDate;
    button.textContent = prayer.completed ? "Completed" : (isAllowedDate ? "Mark complete" : prayer.status);
    button.addEventListener("click", () => completePrayer(prayer.prayerName));

    row.append(info, button);
    prayerList.append(row);
  });
}

function renderPrayerStats(stats) {
  const statsItems = [
    ["Required", stats.expectedCount],
    ["Prayed", stats.completedCount],
    ["Missed", stats.missedCount]
  ];

  prayerStats.replaceChildren(...statsItems.map(([label, value]) => {
    const item = document.createElement("article");
    item.className = "stat-item";
    const statLabel = document.createElement("span");
    statLabel.textContent = label;
    const statValue = document.createElement("strong");
    statValue.textContent = value ?? 0;
    item.append(statLabel, statValue);
    return item;
  }));
}

function renderCalendar(calendar) {
  const firstDay = new Date(calendar.year, calendar.month - 1, 1);
  calendarTitle.textContent = new Intl.DateTimeFormat("en-GB", {
    month: "long",
    year: "numeric"
  }).format(firstDay);

  calendarGrid.replaceChildren();

  for (let i = 0; i < firstDay.getDay(); i += 1) {
    const empty = document.createElement("span");
    empty.className = "calendar-empty";
    calendarGrid.append(empty);
  }

  calendar.days.forEach(day => {
    const date = new Date(day.date);
    const dateKey = toDateKey(date);
    const button = document.createElement("button");
    button.type = "button";
    button.className = "calendar-day";
    button.classList.toggle("selected", dateKey === selectedDate);
    button.classList.toggle("complete", day.isComplete);
    button.classList.toggle("missed", day.missedCount > 0 && date <= new Date());
    button.dataset.date = dateKey;

    const number = document.createElement("strong");
    number.textContent = date.getDate();
    const detail = document.createElement("span");
    detail.textContent = `${day.completedCount}/${day.expectedCount || 5}`;

    button.append(number, detail);
    button.addEventListener("click", () => selectCalendarDate(dateKey));
    calendarGrid.append(button);
  });
}

async function selectCalendarDate(dateKey) {
  // Only allow selecting today and yesterday
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);
  
  const todayKey = toDateKey(today);
  const yesterdayKey = toDateKey(yesterday);
  
  if (dateKey !== todayKey && dateKey !== yesterdayKey) {
    setPrayerMessage("You can only mark prayers for today and yesterday.");
    return;
  }
  
  selectedDate = dateKey;
  await Promise.all([
    loadPrayerDate(dateKey),
    loadPrayerStats(),
    loadCalendar()
  ]);
}

async function setStatsPeriod(period) {
  selectedPeriod = period;
  periodTabs.forEach(button => button.classList.toggle("active", button.dataset.period === selectedPeriod));
  await loadPrayerStats();
}

async function changeCalendarMonth(direction) {
  calendarDate = new Date(calendarDate.getFullYear(), calendarDate.getMonth() + direction, 1);
  await loadCalendar();
}

function showPrayerPanel() {
  authPanel.classList.add("hidden");
  adminPanel.classList.add("hidden");
  prayerPanel.classList.remove("hidden");
  adminButton.classList.toggle("hidden", !state.isAdmin);
  updateSalaam();
}

async function showAdminPanel() {
  authPanel.classList.add("hidden");
  prayerPanel.classList.add("hidden");
  adminPanel.classList.remove("hidden");
  await loadAdmin();
}

function logout() {
  state.token = "";
  state.userName = "";
  state.userId = "";
  state.isAdmin = false;
  localStorage.removeItem("imaan_token");
  localStorage.removeItem("imaan_user_id");
  localStorage.removeItem("imaan_user_name");
  localStorage.removeItem("imaan_is_admin");
  prayerPanel.classList.add("hidden");
  adminPanel.classList.add("hidden");
  authPanel.classList.remove("hidden");
}

async function loadAdmin() {
  setAdminMessage("");

  const [summaryResponse, usersResponse] = await Promise.all([
    request("/Admin/summary", { method: "GET", auth: true }),
    request("/Admin/users", { method: "GET", auth: true })
  ]);

  if (summaryResponse.status === 401 || summaryResponse.status === 403 || usersResponse.status === 401 || usersResponse.status === 403) {
    setAdminMessage("Admin access is required.");
    return;
  }

  if (!summaryResponse.ok || !usersResponse.ok) {
    setAdminMessage("Could not load admin data.");
    return;
  }

  renderAdminSummary(await summaryResponse.json());
  renderAdminUsers(await usersResponse.json());
}

function renderAdminSummary(summary) {
  const items = [
    ["Users", summary.totalUsers],
    ["Prayer logs", summary.totalPrayerLogs],
    ["Perfect today", summary.perfectDaysToday],
    ["Total points", summary.totalPoints]
  ];

  adminSummary.replaceChildren(...items.map(([label, value]) => {
    const card = document.createElement("article");
    card.className = "summary-item";
    card.innerHTML = `<span>${label}</span><strong>${value ?? 0}</strong>`;
    return card;
  }));
}

function renderAdminUsers(users) {
  adminUsers.replaceChildren();

  users.forEach(user => {
    const row = document.createElement("article");
    row.className = "admin-user";
    row.dataset.userId = user.id;

    const fields = document.createElement("div");
    fields.className = "admin-fields";
    fields.append(
      adminInput("Full name", "fullName", user.fullName),
      adminInput("Phone", "phoneNumber", user.phoneNumber || ""),
      adminInput("City", "city", user.city),
      adminInput("Country", "country", user.country),
      adminInput("Points", "totalPoints", user.totalPoints, "number"),
      adminInput("Level", "imaanLevel", user.imaanLevel)
    );

    const meta = document.createElement("div");
    meta.className = "admin-meta";
    const email = document.createElement("strong");
    email.textContent = user.email;
    const details = document.createElement("span");
    details.textContent = `${user.isAdmin ? "Admin" : "User"} - ${user.prayerLogCount} logs`;
    meta.append(email, details);

    const actions = document.createElement("div");
    actions.className = "admin-actions";
    actions.append(
      adminAction("Save", () => saveAdminUser(row)),
      adminAction("Password", () => resetAdminPassword(user.id)),
      adminAction(user.isAdmin ? "Remove admin" : "Make admin", () => toggleAdminRole(user)),
      adminAction("Delete", () => deleteAdminUser(user), "danger")
    );

    row.append(meta, fields, actions);
    adminUsers.append(row);
  });
}

function adminInput(labelText, name, inputValue, type = "text") {
  const label = document.createElement("label");
  label.textContent = labelText;

  const input = document.createElement("input");
  input.name = name;
  input.type = type;
  input.value = inputValue ?? "";

  label.append(input);
  return label;
}

function adminAction(text, action, variant = "") {
  const button = document.createElement("button");
  button.type = "button";
  button.className = variant === "danger" ? "danger-button" : "ghost";
  button.textContent = text;
  button.addEventListener("click", action);
  return button;
}

async function saveAdminUser(row) {
  const userId = row.dataset.userId;
  const formValue = name => row.querySelector(`[name="${name}"]`).value.trim();
  const response = await request(`/Admin/users/${userId}`, {
    method: "PUT",
    auth: true,
    body: {
      fullName: formValue("fullName"),
      phoneNumber: formValue("phoneNumber"),
      city: formValue("city"),
      country: formValue("country"),
      totalPoints: Number(formValue("totalPoints")) || 0,
      imaanLevel: formValue("imaanLevel")
    }
  });

  if (!response.ok) {
    setAdminMessage(await errorText(response) || "Could not save user.");
    return;
  }

  setAdminMessage("User saved.", true);
  showToast("User saved.");
}

async function resetAdminPassword(userId) {
  const newPassword = prompt("New password");
  if (!newPassword) return;

  const response = await request(`/Admin/users/${userId}/reset-password`, {
    method: "POST",
    auth: true,
    body: { newPassword }
  });

  if (!response.ok) {
    setAdminMessage(await errorText(response) || "Could not reset password.");
    return;
  }

  setAdminMessage("Password reset.", true);
  showToast("Password reset.");
}

async function toggleAdminRole(user) {
  const path = user.isAdmin ? "remove-admin" : "make-admin";
  const response = await request(`/Admin/users/${user.id}/${path}`, { method: "POST", auth: true });

  if (!response.ok) {
    setAdminMessage(await errorText(response) || "Could not update admin access.");
    return;
  }

  await loadAdmin();
  showToast("Admin access updated.");
}

async function deleteAdminUser(user) {
  if (!confirm(`Delete ${user.email}?`)) return;

  const response = await request(`/Admin/users/${user.id}`, { method: "DELETE", auth: true });

  if (!response.ok) {
    setAdminMessage(await errorText(response) || "Could not delete user.");
    return;
  }

  await loadAdmin();
  showToast("User deleted.");
}

async function request(path, options = {}) {
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

function toDateKey(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDisplayDate(date) {
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "short",
    day: "numeric",
    month: "long",
    year: "numeric"
  }).format(date);
}

function setAuthMessage(text, ok = false) {
  authMessage.textContent = text;
  authMessage.classList.toggle("ok", ok);
}

function setPrayerMessage(text) {
  prayerMessage.textContent = text;
}

function setAdminMessage(text, ok = false) {
  adminMessage.textContent = text;
  adminMessage.classList.toggle("ok", ok);
}

function toggleDateCard() {
  dateCard.classList.add("rotating");
  window.setTimeout(() => {
    showingHijriDate = !showingHijriDate;
    renderDateCard();
    dateCard.classList.toggle("hijri", showingHijriDate);
  }, 140);
  window.setTimeout(() => dateCard.classList.remove("rotating"), 320);
}

function renderDateCard() {
  const today = new Date();

  if (showingHijriDate) {
    dateCardValue.textContent = formatHijriDate(today);
    dateCard.setAttribute("aria-label", "Show English date");
    return;
  }

  dateCardValue.textContent = new Intl.DateTimeFormat("en-GB", {
    weekday: "short",
    day: "numeric",
    month: "long",
    year: "numeric"
  }).format(today);
  dateCard.setAttribute("aria-label", "Show Arabic calendar date");
}

function formatHijriDate(date) {
  try {
    const parts = new Intl.DateTimeFormat("en-GB-u-ca-islamic-umalqura", {
      weekday: "short",
      day: "numeric",
      month: "long",
      year: "numeric",
      era: "short"
    }).formatToParts(date);

    const value = type => parts.find(part => part.type === type)?.value || "";
    return `${value("weekday")}, ${value("day")} ${normalizeHijriMonth(value("month"))} ${value("year")} ${value("era")}`.trim();
  } catch {
    return new Intl.DateTimeFormat("en-GB-u-ca-islamic", {
      weekday: "short",
      day: "numeric",
      month: "long",
      year: "numeric",
      era: "short"
    }).format(date);
  }
}

function normalizeHijriMonth(month) {
  return month
    .replace("Dhuʻl", "Dhu al")
    .replace("Dhu al-Qiʻdah", "Dhu al-Qidah");
}

function togglePassword(button) {
  const input = document.querySelector(button.dataset.togglePassword);
  const isPassword = input.type === "password";
  input.type = isPassword ? "text" : "password";
  button.classList.toggle("active", isPassword);
  button.setAttribute("aria-label", isPassword ? "Hide password" : "Show password");
}

function setLoginLoading(isLoading) {
  if (!loginButton) return;
  loginButton.disabled = isLoading;
  loginButton.classList.toggle("loading", isLoading);
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

function hydrateAuthFromToken() {
  const payload = readJwtPayload(state.token);
  const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role || "";
  const roles = Array.isArray(role) ? role : [role];
  state.isAdmin = state.isAdmin || roles.includes("Admin");
  state.userId = state.userId || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || payload.sub || "";
  state.userName = state.userName || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || payload.name || "";
}

function readJwtName(token) {
  const payload = readJwtPayload(token);
  return payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || payload.name || "";
}

function readJwtPayload(token) {
  try {
    const encoded = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = encoded.padEnd(encoded.length + (4 - encoded.length % 4) % 4, "=");
    return JSON.parse(atob(padded));
  } catch {
    return {};
  }
}
