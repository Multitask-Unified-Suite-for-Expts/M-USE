mergeInto(LibraryManager.library, {
  NotifyParentTasksComplete: function () {
    console.log("📤 JSlib: Unity says tasks complete — notifying parent window...");

    if (window.parent && window.parent !== window) {
      window.parent.postMessage({ unityComplete: true }, "*");
    } else {
      console.warn("⚠️ No parent window found to notify.");
    }
  }
});
