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
        const clearRun = trigger.getAttribute("data-confirm-clear-run") === "true";

        pendingAction = null;

        if (formId) {
            pendingAction = () => {
                if (clearRun && window.workouts?.run?.planId && window.workouts?.run?.dayId) {
                    sessionStorage.removeItem(`workouts.run.${window.workouts.run.planId}.${window.workouts.run.dayId}`);
                }
                const form = document.getElementById(formId);
                if (form) {
                    form.submit();
                }
            };
        } else if (href) {
            pendingAction = () => {
                if (clearRun && window.workouts?.run?.planId && window.workouts?.run?.dayId) {
                    sessionStorage.removeItem(`workouts.run.${window.workouts.run.planId}.${window.workouts.run.dayId}`);
                }
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
    const initTrainingRun = () => {
        const run = window.workouts && window.workouts.run;
        const stage = document.getElementById("trainingStage");
        if (!run || !stage) {
            return;
        }

        if (stage.getAttribute("data-run-ready") === "true") {
            return;
        }
        stage.setAttribute("data-run-ready", "true");

        const exercises = Array.isArray(run.exercises) ? run.exercises : [];
        if (exercises.length === 0) {
            return;
        }

        const exerciseSection = document.getElementById("trainingExercise");
        const restSection = document.getElementById("trainingRest");
        const completeSection = document.getElementById("trainingComplete");
        const progressEl = document.getElementById("trainingProgress");
        const titleEl = document.getElementById("trainingTitle");
        const descriptionEl = document.getElementById("trainingDescription");
        const imageWrap = document.getElementById("trainingImageWrap");
        const repsEl = document.getElementById("trainingReps");
        const timerEl = document.getElementById("trainingTimer");
        const notesEl = document.getElementById("trainingNotes");
        const restTimerEl = document.getElementById("trainingRestTimer");
        const restNextEl = document.getElementById("trainingRestNext");
        const restMinus = document.getElementById("restMinus");
        const restPlus = document.getElementById("restPlus");
        const prevButton = document.getElementById("trainingPrev");
        const nextButton = document.getElementById("trainingNext");

        if (!exerciseSection || !restSection || !completeSection || !progressEl || !titleEl || !descriptionEl ||
            !imageWrap || !repsEl || !timerEl || !notesEl || !restTimerEl || !restNextEl || !restMinus || !restPlus ||
            !prevButton || !nextButton) {
            return;
        }

        const buildSteps = (items) => {
            const steps = [];
            let exerciseIndex = 0;

            items.forEach((exercise) => {
                const sets = Number.isFinite(exercise.sets) ? Math.max(1, exercise.sets) : 1;
                const restSeconds = Number.isFinite(exercise.restSeconds) ? Math.max(0, exercise.restSeconds) : 0;

                for (let setIndex = 1; setIndex <= sets; setIndex += 1) {
                    exerciseIndex += 1;
                    steps.push({
                        type: "exercise",
                        exercise,
                        setIndex,
                        setCount: sets,
                        exerciseStepIndex: exerciseIndex
                    });

                    if (restSeconds > 0) {
                        steps.push({
                            type: "rest",
                            exercise,
                            restSeconds
                        });
                    }
                }
            });

            if (steps.length > 0 && steps[steps.length - 1].type === "rest") {
                steps.pop();
            }

            return steps;
        };

        const steps = buildSteps(exercises);
        if (steps.length === 0) {
            return;
        }

        steps.push({ type: "complete" });

        const totalExerciseSteps = steps.filter((step) => step.type === "exercise").length;
        const storageKey = `workouts.run.${run.planId}.${run.dayId}`;
        let currentIndex = 0;
        let elapsedSeconds = 0;
        let remainingSeconds = 0;
        let lastUpdated = Date.now();

        const formatTime = (seconds) => {
            const value = Math.max(0, Math.floor(seconds));
            const mins = Math.floor(value / 60).toString().padStart(2, "0");
            const secs = (value % 60).toString().padStart(2, "0");
            return `${mins}:${secs}`;
        };

        const setVisibility = (element, isVisible) => {
            element.classList.toggle("d-none", !isVisible);
        };

        const setText = (element, value) => {
            if (!value) {
                element.textContent = "";
                element.classList.add("d-none");
                return;
            }
            element.textContent = value;
            element.classList.remove("d-none");
        };

        const saveState = () => {
            sessionStorage.setItem(
                storageKey,
                JSON.stringify({
                    planId: run.planId,
                    dayId: run.dayId,
                    index: currentIndex,
                    elapsedSeconds,
                    remainingSeconds,
                    lastUpdated: Date.now()
                })
            );
        };

        const resetTimersForStep = (step) => {
            elapsedSeconds = 0;
            remainingSeconds = 0;
            if (step.type === "rest") {
                remainingSeconds = step.restSeconds;
            }
            lastUpdated = Date.now();
        };

        const loadState = () => {
            const raw = sessionStorage.getItem(storageKey);
            if (!raw) {
                return false;
            }

            try {
                const saved = JSON.parse(raw);
                if (saved.planId !== run.planId || saved.dayId !== run.dayId) {
                    return false;
                }

                if (!Number.isFinite(saved.index) || saved.index < 0 || saved.index >= steps.length) {
                    return false;
                }

                currentIndex = saved.index;
                const now = Date.now();
                const delta = Number.isFinite(saved.lastUpdated)
                    ? Math.max(0, Math.floor((now - saved.lastUpdated) / 1000))
                    : 0;

                const step = steps[currentIndex];
                if (step.type === "exercise") {
                    elapsedSeconds = Math.max(0, Number(saved.elapsedSeconds) || 0) + delta;
                } else if (step.type === "rest") {
                    const baseRemaining = Number.isFinite(saved.remainingSeconds)
                        ? saved.remainingSeconds
                        : step.restSeconds;
                    remainingSeconds = Math.max(0, baseRemaining - delta);
                }

                return true;
            } catch (error) {
                return false;
            }
        };

        const updateExerciseView = (step) => {
            const exercise = step.exercise;
            setVisibility(exerciseSection, true);
            setVisibility(restSection, false);
            setVisibility(completeSection, false);

            progressEl.textContent = `Exercise ${step.exerciseStepIndex} of ${totalExerciseSteps} · Set ${step.setIndex} of ${step.setCount}`;
            titleEl.textContent = exercise.name;
            setText(descriptionEl, exercise.description);

            if (exercise.imageUrl) {
                imageWrap.innerHTML = "";
                const img = document.createElement("img");
                img.src = exercise.imageUrl;
                img.alt = exercise.name;
                img.loading = "lazy";
                imageWrap.appendChild(img);
                setVisibility(imageWrap, true);
            } else {
                imageWrap.innerHTML = "";
                setVisibility(imageWrap, false);
            }

            repsEl.textContent = `× ${exercise.repetitions}`;
            timerEl.textContent = `Elapsed: ${formatTime(elapsedSeconds)}`;
            setText(notesEl, exercise.notes ? `Notes:\n${exercise.notes}` : "");
        };

        const updateRestView = (step) => {
            setVisibility(exerciseSection, false);
            setVisibility(restSection, true);
            setVisibility(completeSection, false);

            progressEl.textContent = "Rest";
            restTimerEl.textContent = formatTime(remainingSeconds);

            const nextStep = steps[currentIndex + 1];
            if (nextStep && nextStep.type === "exercise") {
                restNextEl.textContent = `Up next: ${nextStep.exercise.name} (Set ${nextStep.setIndex} of ${nextStep.setCount})`;
            } else {
                restNextEl.textContent = "Up next: Finish";
            }
        };

        const updateCompleteView = () => {
            setVisibility(exerciseSection, false);
            setVisibility(restSection, false);
            setVisibility(completeSection, true);
            progressEl.textContent = "Complete";
        };

        const renderStep = () => {
            const step = steps[currentIndex];
            prevButton.disabled = currentIndex === 0;

            if (step.type === "complete") {
                nextButton.textContent = "Back to plan";
                updateCompleteView();
            } else {
                nextButton.innerHTML = "Next <span aria-hidden=\"true\">→</span>";
                if (step.type === "exercise") {
                    updateExerciseView(step);
                } else if (step.type === "rest") {
                    updateRestView(step);
                }
            }

            stage.classList.remove("training-animate");
            void stage.offsetWidth;
            stage.classList.add("training-animate");
        };

        const goToIndex = (index) => {
            if (index < 0 || index >= steps.length) {
                return;
            }
            currentIndex = index;
            resetTimersForStep(steps[currentIndex]);
            renderStep();
            saveState();
        };

        const goNext = () => {
            if (currentIndex >= steps.length - 1) {
                return;
            }
            goToIndex(currentIndex + 1);
        };

        const goPrev = () => {
            if (currentIndex <= 0) {
                return;
            }
            goToIndex(currentIndex - 1);
        };

        prevButton.addEventListener("click", () => {
            goPrev();
        });

        nextButton.addEventListener("click", () => {
            const step = steps[currentIndex];
            if (step.type === "complete") {
                sessionStorage.removeItem(storageKey);
                if (run.detailsUrl) {
                    window.location.href = run.detailsUrl;
                }
                return;
            }
            goNext();
        });

        restMinus.addEventListener("click", () => {
            if (steps[currentIndex].type !== "rest") {
                return;
            }
            remainingSeconds = Math.max(0, remainingSeconds - 10);
            restTimerEl.textContent = formatTime(remainingSeconds);
            saveState();
        });

        restPlus.addEventListener("click", () => {
            if (steps[currentIndex].type !== "rest") {
                return;
            }
            remainingSeconds = remainingSeconds + 10;
            restTimerEl.textContent = formatTime(remainingSeconds);
            saveState();
        });

        const bootedFromStorage = loadState();
        if (!bootedFromStorage) {
            resetTimersForStep(steps[currentIndex]);
        }

        renderStep();
        saveState();

        setInterval(() => {
            const step = steps[currentIndex];
            if (step.type === "exercise") {
                elapsedSeconds += 1;
                timerEl.textContent = `Elapsed: ${formatTime(elapsedSeconds)}`;
            } else if (step.type === "rest") {
                remainingSeconds = Math.max(0, remainingSeconds - 1);
                restTimerEl.textContent = formatTime(remainingSeconds);
                if (remainingSeconds <= 0) {
                    goNext();
                    return;
                }
            }
            saveState();
        }, 1000);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initTrainingRun, { once: true });
    } else {
        initTrainingRun();
    }
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
                            const baseName = file.name.replace(/\.[^/.]+$/, "");
                            const webpName = `${baseName || "image"}.webp`;
                            const compressed = new File([blob], webpName, { type: "image/webp" });
                            resolve(compressed);
                        },
                        "image/webp",
                        quality
                    );
                    URL.revokeObjectURL(img.src);
                };
                img.onerror = () => resolve(file);
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
