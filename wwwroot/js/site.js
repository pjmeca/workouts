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
    const list = document.getElementById("dayList");
    const tokenInput = document.querySelector("#dayReorderToken input[name='__RequestVerificationToken']");

    if (!list || !tokenInput) {
        return;
    }

    const planId = list.getAttribute("data-plan-id");
    if (!planId) {
        return;
    }

    const persistExerciseOrder = async (dayId, tbody) => {
        const orderedIds = [...tbody.querySelectorAll("tr[data-exercise-id]")]
            .map((row) => row.getAttribute("data-exercise-id"))
            .filter(Boolean)
            .map((value) => Number.parseInt(value, 10))
            .filter(Number.isFinite);

        if (orderedIds.length === 0) {
            return;
        }

        try {
            const response = await fetch("?handler=ReorderExercises", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify({ planId, dayId: Number.parseInt(dayId, 10), orderedIds })
            });

            if (!response.ok) {
                console.warn("Failed to reorder exercises.");
            }
        } catch (error) {
            console.warn("Failed to reorder exercises.", error);
        }
    };

    list.addEventListener("click", async (event) => {
        const moveButton = event.target.closest(".exercise-move");
        if (!moveButton) {
            return;
        }

        event.preventDefault();

        const row = moveButton.closest("tr[data-exercise-id]");
        if (!row) {
            return;
        }

        const tbody = row.parentElement;
        if (!tbody) {
            return;
        }

        const dayId = tbody.getAttribute("data-day-id");
        if (!dayId) {
            return;
        }

        const delta = Number.parseInt(moveButton.getAttribute("data-delta") || "0", 10);
        if (!Number.isFinite(delta) || delta === 0) {
            return;
        }

        if (delta < 0) {
            let prev = row.previousElementSibling;
            while (prev && !prev.hasAttribute("data-exercise-id")) {
                prev = prev.previousElementSibling;
            }
            if (prev) {
                tbody.insertBefore(row, prev);
                await persistExerciseOrder(dayId, tbody);
            }
        } else {
            let next = row.nextElementSibling;
            while (next && !next.hasAttribute("data-exercise-id")) {
                next = next.nextElementSibling;
            }
            if (next) {
                tbody.insertBefore(row, next.nextSibling);
                await persistExerciseOrder(dayId, tbody);
            }
        }
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

(() => {
    const modalElement = document.getElementById("exerciseModal");
    const modalBody = document.getElementById("exerciseModalBody");
    const modalTitle = document.getElementById("exerciseModalTitle");

    if (!modalElement || !modalBody || !modalTitle || !window.bootstrap) {
        return;
    }

    const modal = new bootstrap.Modal(modalElement);

    const initImageCompression = (container) => {
        const input = container.querySelector("input[data-compress-image]");
        if (!input) {
            return;
        }

        const compressImage = (file, maxSize = 1600, quality = 0.8) =>
            new Promise((resolve, reject) => {
                if (!file.type.startsWith("image/")) {
                    resolve(file);
                    return;
                }

                const img = new Image();
                img.onload = () => {
                    const ratio = Math.min(1, maxSize / Math.max(img.width, img.height));
                    const canvas = document.createElement("canvas");
                    canvas.width = Math.round(img.width * ratio);
                    canvas.height = Math.round(img.height * ratio);
                    const ctx = canvas.getContext("2d");
                    if (!ctx) {
                        resolve(file);
                        return;
                    }
                    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                    canvas.toBlob(
                        (blob) => {
                            if (!blob) {
                                resolve(file);
                                return;
                            }
                            const compressed = new File([blob], file.name, { type: blob.type });
                            resolve(compressed);
                        },
                        "image/jpeg",
                        quality
                    );
                };
                img.onerror = reject;
                img.src = URL.createObjectURL(file);
            });

        input.addEventListener("change", async () => {
            const file = input.files && input.files[0];
            if (!file) {
                return;
            }

            try {
                const compressed = await compressImage(file);
                if (compressed !== file) {
                    const dataTransfer = new DataTransfer();
                    dataTransfer.items.add(compressed);
                    input.files = dataTransfer.files;
                }
            } catch (error) {
                console.warn("Image compression failed.", error);
            }
        });
    };

    document.addEventListener("click", async (event) => {
        const trigger = event.target.closest("[data-exercise-modal]");
        if (!trigger) {
            return;
        }

        event.preventDefault();

        const planId = trigger.getAttribute("data-plan-id");
        const dayId = trigger.getAttribute("data-day-id");
        const exerciseId = trigger.getAttribute("data-exercise-id");

        if (!planId || !dayId) {
            return;
        }

        const url = new URL(window.location.href);
        url.searchParams.set("handler", "ExerciseForm");
        url.searchParams.set("planId", planId);
        url.searchParams.set("dayId", dayId);
        if (exerciseId) {
            url.searchParams.set("exerciseId", exerciseId);
        }

        modalBody.innerHTML = "<p class=\"text-muted\">Loading...</p>";
        modalTitle.textContent = exerciseId ? "Edit exercise" : "Add exercise";
        modal.show();

        try {
            const response = await fetch(url.toString(), {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            modalBody.innerHTML = await response.text();
            initImageCompression(modalBody);
        } catch (error) {
            modalBody.innerHTML = "<p class=\"text-danger\">Failed to load form.</p>";
        }
    });

    modalBody.addEventListener("submit", async (event) => {
        const form = event.target.closest("form[data-exercise-form]");
        if (!form) {
            return;
        }

        event.preventDefault();

        const formData = new FormData(form);
        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: formData
            });

            const contentType = response.headers.get("content-type") || "";
            if (contentType.includes("application/json")) {
                const result = await response.json();
                if (result.success) {
                    modal.hide();
                    window.location.reload();
                }
                return;
            }

            modalBody.innerHTML = await response.text();
            initImageCompression(modalBody);
        } catch (error) {
            modalBody.innerHTML = "<p class=\"text-danger\">Failed to save exercise.</p>";
        }
    });
})();

(() => {
    document.addEventListener("keydown", (event) => {
        const input = event.target.closest("input[type='number'][data-integer-only]");
        if (!input) {
            return;
        }

        if (["e", "E", "+", "-", "."].includes(event.key)) {
            event.preventDefault();
        }
    });
})();

(() => {
    const list = document.getElementById("dayList");
    const tokenInput = document.querySelector("#dayReorderToken input[name='__RequestVerificationToken']");

    if (!list || !tokenInput) {
        return;
    }

    const planId = list.getAttribute("data-plan-id");
    if (!planId) {
        return;
    }

    const persistOrder = async () => {
        const orderedIds = [...list.querySelectorAll(".day-card")]
            .map((card) => card.getAttribute("data-day-id"))
            .filter(Boolean)
            .map((value) => Number.parseInt(value, 10))
            .filter(Number.isFinite);

        if (orderedIds.length === 0) {
            return;
        }

        try {
            const response = await fetch("?handler=ReorderDays", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify({ planId, orderedIds })
            });

            if (!response.ok) {
                console.warn("Failed to reorder days.");
            }
        } catch (error) {
            console.warn("Failed to reorder days.", error);
        }
    };

    const getDragAfterElement = (container, y) => {
        const draggableElements = [...container.querySelectorAll(".day-card:not(.dragging)")];

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
        const card = event.target.closest(".day-card");
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

        dragging = event.target.closest(".day-card");
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
        const moveButton = event.target.closest(".day-move");
        if (!moveButton) {
            return;
        }

        event.preventDefault();

        const card = moveButton.closest(".day-card");
        if (!card) {
            return;
        }

        const delta = Number.parseInt(moveButton.getAttribute("data-delta") || "0", 10);
        if (!Number.isFinite(delta) || delta === 0) {
            return;
        }

        if (delta < 0) {
            const prev = card.previousElementSibling;
            if (prev && prev.classList.contains("day-card")) {
                list.insertBefore(card, prev);
                await persistOrder();
            }
        } else {
            const next = card.nextElementSibling;
            if (next && next.classList.contains("day-card")) {
                list.insertBefore(card, next.nextSibling);
                await persistOrder();
            }
        }
    });
})();
