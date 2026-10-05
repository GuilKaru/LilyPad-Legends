mergeInto(LibraryManager.library, {
  SendActionToBackend: function (actionStringPtr) {
    var actionStr = UTF8ToString(actionStringPtr);
    var event = new CustomEvent("LilypadPlayerAction", { detail: actionStr });
    window.dispatchEvent(event);
  },

  SendMatchRequestToJS: function () {
    var event = new CustomEvent("LilypadRequestMatch", { detail: "" });
    window.dispatchEvent(event);
  }
});