// Calls from WebBridge.cs into the page (window.RQ is defined by the RocketQuiz WebGL template).
mergeInto(LibraryManager.library, {
  RQ_Ready: function () {
    if (window.RQ) window.RQ.ready();
  },
  RQ_ShowAnswer: function (placeholder) {
    if (window.RQ) window.RQ.showAnswer(UTF8ToString(placeholder));
  },
  RQ_HideAnswer: function () {
    if (window.RQ) window.RQ.hideAnswer();
  },
  RQ_ClearAnswer: function () {
    if (window.RQ) window.RQ.clearAnswer();
  },
});
