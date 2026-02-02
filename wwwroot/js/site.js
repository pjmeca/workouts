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

(() => {
    const list = document.getElementById("planList");
    const tokenInput = document.querySelector("#planReorderToken input[name='__RequestVerificationToken']");

    if (!list || !tokenInput) {
        return;
    }

    const persistOrder = async () => {
        const orderedIds = [...list.querySelectorAll(".plan-card")]
            .map((card) => card.getAttribute("data-plan-id"))
            .filter(Boolean);

        if (orderedIds.length === 0) {
            return;
        }

        try {
            const response = await fetch("?handler=Reorder", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify({ orderedIds })
            });

            if (!response.ok) {
                console.warn("Failed to reorder plans.");
            }
        } catch (error) {
            console.warn("Failed to reorder plans.", error);
        }
    };

    const getDragAfterElement = (container, y) => {
        const draggableElements = [...container.querySelectorAll(".plan-card:not(.dragging)")];

        return draggableElements.reduce(
            (closest, child) => {
                const box = child.getBoundingClientRect();
                const offset = y - box.top - box.height / 2;
                if (offset < 0 && offset > closest.offset) {
                    return { offset, element: child };
                }
                return closest;
            },
            { offset: Number.NEGATIVE_INFINITY, element: null }
        ).element;
    };

    let dragging = null;
    let allowDrag = false;

    list.addEventListener("pointerdown", (event) => {
        const card = event.target.closest(".plan-card");
        if (!card || event.target.closest("button") || event.target.closest("a") || event.target.closest("form")) {
            return;
        }
        allowDrag = true;
    });

    list.addEventListener("dragstart", (event) => {
        if (!allowDrag) {
            event.preventDefault();
            return;
        }

        dragging = event.target.closest(".plan-card");
        if (!dragging) {
            event.preventDefault();
            return;
        }

        dragging.classList.add("dragging");
        event.dataTransfer.effectAllowed = "move";
    });

    list.addEventListener("dragend", () => {
        if (dragging) {
            dragging.classList.remove("dragging");
        }
        dragging = null;
        allowDrag = false;
    });

    list.addEventListener("dragover", (event) => {
        if (!dragging) {
            return;
        }
        event.preventDefault();
        const afterElement = getDragAfterElement(list, event.clientY);
        if (afterElement == null) {
            list.appendChild(dragging);
        } else {
            list.insertBefore(dragging, afterElement);
        }
    });

    list.addEventListener("drop", async (event) => {
        if (!dragging) {
            return;
        }
        event.preventDefault();

        await persistOrder();
    });

    list.addEventListener("click", async (event) => {
        const moveButton = event.target.closest(".plan-move");
        if (!moveButton) {
            return;
        }

        event.preventDefault();

        const card = moveButton.closest(".plan-card");
        if (!card) {
            return;
        }

        const delta = Number.parseInt(moveButton.getAttribute("data-delta") || "0", 10);
        if (!Number.isFinite(delta) || delta === 0) {
            return;
        }

        if (delta < 0) {
            const prev = card.previousElementSibling;
            if (prev && prev.classList.contains("plan-card")) {
                list.insertBefore(card, prev);
                await persistOrder();
            }
        } else {
            const next = card.nextElementSibling;
            if (next && next.classList.contains("plan-card")) {
                list.insertBefore(card, next.nextSibling);
                await persistOrder();
            }
        }
    });
})();
