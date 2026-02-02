(() => {
    const modalElement = document.getElementById("confirmModal");
    const modalMessage = document.getElementById("confirmModalMessage");
    const modalAction = document.getElementById("confirmModalAction");

    if (!modalElement || !modalMessage || !modalAction || !window.bootstrap) {
        return;
    }

    const modal = new bootstrap.Modal(modalElement);
    let pendingAction = null;

    document.addEventListener("click", (event) => {
        const trigger = event.target.closest("[data-confirm]");
        if (!trigger) {
            return;
        }

        event.preventDefault();

        const message = trigger.getAttribute("data-confirm-message") || "Are you sure?";
        modalMessage.textContent = message;

        const formId = trigger.getAttribute("data-confirm-form");
        const href = trigger.getAttribute("data-confirm-href");

        pendingAction = null;

        if (formId) {
            pendingAction = () => {
                const form = document.getElementById(formId);
                if (form) {
                    form.submit();
                }
            };
        } else if (href) {
            pendingAction = () => {
                window.location.href = href;
            };
        }

        modal.show();
    });

    modalAction.addEventListener("click", () => {
        if (pendingAction) {
            pendingAction();
        }
        modal.hide();
    });
})();
