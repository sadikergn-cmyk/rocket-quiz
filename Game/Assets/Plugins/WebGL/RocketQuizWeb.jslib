// Calls from WebBridge.cs into the page: bridge contract v1 (window.RQ). The RocketQuiz WebGL template and the
// Nadide Next.js host both define window.RQ; every call is guarded so a page without a function keeps working,
// and a throwing page function never reaches the game.
mergeInto(LibraryManager.library, {
  RQ_Ready: function () {
    if (window.RQ && typeof window.RQ.ready === "function") window.RQ.ready();
  },
  RQ_ShowAnswer: function (placeholder) {
    if (window.RQ && typeof window.RQ.showAnswer === "function") window.RQ.showAnswer(UTF8ToString(placeholder));
  },
  RQ_HideAnswer: function () {
    if (window.RQ && typeof window.RQ.hideAnswer === "function") window.RQ.hideAnswer();
  },
  RQ_ClearAnswer: function () {
    if (window.RQ && typeof window.RQ.clearAnswer === "function") window.RQ.clearAnswer();
  },
  // Returns 1 when the page took the share (its result comes back through OnWebShareResult), 0 otherwise.
  RQ_Share: function (json) {
    if (!window.RQ || typeof window.RQ.share !== "function") return 0;
    try {
      window.RQ.share(UTF8ToString(json));
      return 1;
    } catch (e) {
      console.warn("RQ.share", e);
      return 0;
    }
  },
  RQ_Announce: function (text) {
    if (!window.RQ || typeof window.RQ.announce !== "function") return;
    try { window.RQ.announce(UTF8ToString(text)); } catch (e) { console.warn("RQ.announce", e); }
  },
  RQ_BetaRequired: function () {
    if (!window.RQ || typeof window.RQ.betaRequired !== "function") return;
    try { window.RQ.betaRequired(); } catch (e) { console.warn("RQ.betaRequired", e); }
  },
});
