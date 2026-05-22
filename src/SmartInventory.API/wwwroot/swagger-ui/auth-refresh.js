(() => {
  const originalFetch = window.fetch.bind(window);
  let currentAccessToken = null;
  let refreshingPromise = null;

  function getAuthHeaderValue() {
    if (!currentAccessToken) return null;
    return `Bearer ${currentAccessToken}`;
  }

  async function refreshAccessToken() {
    if (refreshingPromise) return refreshingPromise;

    refreshingPromise = (async () => {
      const res = await originalFetch("/api/auth/refresh", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: "" }),
        credentials: "same-origin",
      });

      if (!res.ok) throw new Error("Refresh failed");

      const json = await res.json();
      const token = json && json.data && json.data.token;
      if (!token) throw new Error("No token in refresh response");

      currentAccessToken = token;

      try {
        if (window.ui && window.ui.preauthorizeApiKey) {
          window.ui.preauthorizeApiKey("Bearer", token);
        }
      } catch {}

      return token;
    })();

    try {
      return await refreshingPromise;
    } finally {
      refreshingPromise = null;
    }
  }

  function shouldSkipRefresh(url) {
    if (!url) return true;
    const u = url.toString();
    return u.includes("/api/auth/login") || u.includes("/api/auth/refresh") || u.includes("/api/auth/logout");
  }

  window.fetch = async (input, init = {}) => {
    const url = typeof input === "string" ? input : input && input.url;
    const headers = new Headers((init && init.headers) || {});

    const existingAuth = headers.get("Authorization");
    if (!existingAuth) {
      const authValue = getAuthHeaderValue();
      if (authValue) headers.set("Authorization", authValue);
    } else if (existingAuth.startsWith("Bearer ")) {
      currentAccessToken = existingAuth.substring("Bearer ".length);
    }

    init.headers = headers;

    if (!init.credentials) {
      init.credentials = "same-origin";
    }

    let res = await originalFetch(input, init);

    if (res.status !== 401 || init.__retried || shouldSkipRefresh(url)) {
      return res;
    }

    try {
      const newToken = await refreshAccessToken();
      const retryInit = { ...init, __retried: true };
      const retryHeaders = new Headers(retryInit.headers || {});
      retryHeaders.set("Authorization", `Bearer ${newToken}`);
      retryInit.headers = retryHeaders;
      retryInit.credentials = "same-origin";

      res = await originalFetch(input, retryInit);
      return res;
    } catch {
      return res;
    }
  };
})();

