(() => {
  const header = document.getElementById("siteHeader");
  const toggle = document.getElementById("navToggle");
  const setOpen = (open) => {
    if (!header || !toggle) return;
    header.classList.toggle("open", open);
    toggle.setAttribute("aria-expanded", open ? "true" : "false");
    toggle.setAttribute("aria-label", open ? "Close menu" : "Open menu");
  };

  if (header && toggle) {
    toggle.addEventListener("click", () => setOpen(!header.classList.contains("open")));
    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") setOpen(false);
    });
  }

  const serviceSelect = document.querySelector("#serviceSelect");
  if (serviceSelect) {
    const form = serviceSelect.closest("form");
    const catalog = [...form.querySelectorAll("[data-tariff-catalog] option")].filter((option) => option.value);
    const tariffInput = form.querySelector("[data-selected-tariff]");
    const amountInput = form.querySelector("[data-amount-input]");
    const amountLabel = form.querySelector("[data-amount-label]");
    const additionalOptions = form.querySelector("[data-additional-options]");
    const rentGroup = form.querySelector('[data-option-group="rent"]');
    const electricityGroup = form.querySelector('[data-option-group="electricity"]');
    const parkingGroup = form.querySelector('[data-option-group="parking"]');
    const rentOption = form.querySelector("[data-rent-option]");
    const electricityOption = form.querySelector("[data-electricity-option]");
    const parkingSummary = form.querySelector("[data-parking-summary]");
    const fixedHint = form.querySelector("[data-fixed-service-hint]");
    const vehicleCount = Number(form.dataset.vehicleCount || 0);
    const currency = (value) => new Intl.NumberFormat("en-ZA", { style: "currency", currency: "ZAR" }).format(value);

    const fillOptions = (select, categoryName, labelFor) => {
      const previousId = tariffInput.value;
      select.replaceChildren(new Option(categoryName === "Rent" ? "Choose a room type" : "Choose a kWh package", ""));
      const matches = catalog.filter((option) => option.dataset.categoryName === categoryName && (!labelFor || labelFor(option)));
      if (categoryName === "Rent") matches.sort((a, b) => (a.dataset.roomType === "Single Room" ? 0 : 1) - (b.dataset.roomType === "Single Room" ? 0 : 1));
      if (categoryName === "Electricity") matches.sort((a, b) => a.dataset.name.localeCompare(b.dataset.name, undefined, { numeric: true }));
      for (const tariff of matches) {
        const label = categoryName === "Rent" ? tariff.dataset.roomType : `${tariff.dataset.name} (${currency(Number(tariff.dataset.amount))})`;
        if (!label) continue;
        const option = new Option(label, tariff.value);
        option.dataset.amount = tariff.dataset.amount;
        select.add(option);
      }
      select.required = true;
      select.disabled = false;
      if ([...select.options].some((option) => option.value === previousId)) select.value = previousId;
      else if (select.options.length > 1) select.selectedIndex = 1;
    };

    const syncServiceOptions = (refreshDependentOptions = false) => {
      const service = serviceSelect.selectedOptions[0]?.textContent.trim() || "";
      const previousTariff = tariffInput.value;
      const rent = service === "Rent";
      const electricity = service === "Electricity";
      const parking = service === "Parking";
      rentGroup.hidden = !rent;
      electricityGroup.hidden = !electricity;
      parkingGroup.hidden = !parking;
      fixedHint.hidden = !(service === "Water" || service === "Refuse Collection");
      fixedHint.textContent = fixedHint.hidden ? "" : "The active service tariff is applied automatically.";
      additionalOptions.hidden = !service;
      rentOption.required = rent;
      electricityOption.required = electricity;
      rentOption.disabled = !rent;
      electricityOption.disabled = !electricity;

      let tariffId = "";
      let baseAmount = 0;
      if (rent) {
        if (refreshDependentOptions) fillOptions(rentOption, "Rent", (option) => Boolean(option.dataset.roomType));
        tariffId = rentOption.value;
        baseAmount = Number(rentOption.selectedOptions[0]?.dataset.amount || 0);
      } else if (electricity) {
        if (refreshDependentOptions) fillOptions(electricityOption, "Electricity");
        tariffId = electricityOption.value;
        baseAmount = Number(electricityOption.selectedOptions[0]?.dataset.amount || 0);
      } else if (service) {
        const tariff = catalog.find((option) => option.dataset.categoryName === service);
        if (tariff) {
          tariffId = tariff.value;
          baseAmount = Number(tariff.dataset.amount || 0);
        }
      }

      if (parking && parkingSummary) {
        const additionalCars = Math.max(0, vehicleCount - 1);
        parkingSummary.textContent = `${additionalCars} additional ${additionalCars === 1 ? "car" : "cars"} × ${currency(baseAmount)} per car.`;
      }
      const amount = parking ? baseAmount * Math.max(0, vehicleCount - 1) : baseAmount;
      tariffInput.value = tariffId;
      amountInput.value = amount > 0 ? String(amount) : "";
      amountLabel.textContent = amount > 0 ? currency(amount) : service ? "Choose an option" : "Choose a service";
      if (previousTariff && tariffId === previousTariff && !baseAmount) amountInput.value = "";
    };

    serviceSelect.addEventListener("change", () => syncServiceOptions(true));
    rentOption.addEventListener("change", () => syncServiceOptions());
    electricityOption.addEventListener("change", () => syncServiceOptions());
    syncServiceOptions(true);
  }

  document.querySelectorAll("[data-tariff-select]").forEach((tariffSelect) => {
    const form = tariffSelect.closest("form");
    const amountInput = form?.querySelector("[data-amount-input]");
    const categoryInput = form?.querySelector("[data-category-input]");
    if (!amountInput || !categoryInput) return;

    const syncTariffAmount = () => {
      const selected = tariffSelect.selectedOptions[0];
      const amount = selected?.value ? Number(selected.dataset.amount) : 0;
      amountInput.value = amount > 0 ? String(amount) : "";
      categoryInput.value = selected?.value ? selected.dataset.category || "" : "";
    };

    tariffSelect.addEventListener("change", syncTariffAmount);
    syncTariffAmount();
  });

  const syncPaymentMethod = () => {
    const method = document.querySelector('[name="PaymentMethod"]:checked')?.value;
    const eftBox = document.getElementById("eftBox");
    const cardBox = document.querySelector("[data-card-box]");
    if (eftBox) eftBox.style.display = method === "EFT" ? "block" : "none";
    if (cardBox) cardBox.style.display = method === "Card" ? "block" : "none";
  };
  document.querySelectorAll('[name="PaymentMethod"]').forEach((radio) => radio.addEventListener("change", syncPaymentMethod));
  syncPaymentMethod();

  document.querySelectorAll("[data-confirm-payment]").forEach((paymentForm) => {
    const paymentModal = document.querySelector("[data-payment-modal]");
    if (!paymentModal) return;
    let submitting = false;
    paymentForm.addEventListener("submit", (event) => {
      if (submitting) return;
      event.preventDefault();
      if (serviceSelect && !serviceSelect.value) { paymentForm.reportValidity(); return; }
      const activeOption = paymentForm.querySelector('[data-rent-option]:not(:disabled), [data-electricity-option]:not(:disabled)');
      if (activeOption && !activeOption.value) { paymentForm.reportValidity(); return; }
      const amount = Number(paymentForm.querySelector("[data-amount-input]")?.value || 0);
      paymentModal.querySelector("[data-confirm-amount]").textContent = new Intl.NumberFormat("en-ZA", { style: "currency", currency: "ZAR" }).format(amount);
      const serviceName = serviceSelect?.selectedOptions[0]?.textContent.trim() || "selected service";
      const selectedOption = activeOption?.selectedOptions[0]?.textContent.trim();
      paymentModal.querySelector("[data-confirm-service]").textContent = selectedOption ? `${serviceName} — ${selectedOption}` : serviceName;
      paymentModal.hidden = false;
    });
    paymentModal.querySelector("[data-cancel-payment]")?.addEventListener("click", () => paymentModal.hidden = true);
    paymentModal.querySelector("[data-accept-payment]")?.addEventListener("click", () => { submitting = true; paymentModal.hidden = true; paymentForm.requestSubmit(); });
    paymentModal.addEventListener("click", (event) => { if (event.target === paymentModal) paymentModal.hidden = true; });
  });

  const roleSelect = document.querySelector("[data-role-select]");
  const idInput = document.querySelector("[data-user-number]");
  const idLabel = document.querySelector("[data-user-number-label]");
  if (roleSelect && idInput && idLabel) {
    const syncId = () => {
      const role = roleSelect.value;
      const [label, length] = role === "Student" ? ["Student Number", 10] : role === "Staff" ? ["Staff Number", 7] : role === "FinancialOfficer" ? ["Officer Number", 7] : ["Admin Number", 7];
      idLabel.textContent = label;
      idInput.maxLength = length;
      idInput.pattern = `[0-9]{${length}}`;
      idInput.inputMode = "numeric";
      idInput.placeholder = `${length} digits`;
    };
    roleSelect.addEventListener("change", syncId);
    syncId();
  }

  const accountType = document.querySelector("[data-account-type]");
  const registerId = document.querySelector("[data-register-number]");
  const registerLabel = document.querySelector("[data-register-number-label]");
  if (accountType && registerId && registerLabel) {
    const sync = () => { const student = accountType.value === "Student"; const length = student ? 10 : 7; registerLabel.textContent = student ? "Student Number" : "Staff Number"; registerId.maxLength = length; registerId.pattern = `[0-9]{${length}}`; registerId.inputMode = "numeric"; registerId.placeholder = `${length} digits`; };
    accountType.addEventListener("change", sync); sync();
  }

  document.querySelectorAll("[data-confirm-create]").forEach((form) => {
    const modal = document.querySelector("[data-create-modal]");
    if (!modal) return;
    let submitting = false;
    form.addEventListener("submit", (event) => { if (submitting) return; event.preventDefault(); if (!form.reportValidity()) return; modal.hidden = false; });
    modal.querySelector("[data-cancel-create]")?.addEventListener("click", () => modal.hidden = true);
    modal.querySelector("[data-accept-create]")?.addEventListener("click", () => { submitting = true; modal.hidden = true; form.requestSubmit(); });
  });

  const signoutModal = document.querySelector("[data-signout-modal]");
  const signoutTrigger = document.querySelector("[data-open-signout]");
  const signoutCancel = signoutModal?.querySelector("[data-cancel-signout]");
  const closeSignout = () => {
    if (!signoutModal) return;
    signoutModal.hidden = true;
    signoutTrigger?.focus();
  };
  signoutTrigger?.addEventListener("click", () => {
    if (!signoutModal) return;
    signoutModal.hidden = false;
    signoutCancel?.focus();
  });
  signoutCancel?.addEventListener("click", closeSignout);
  signoutModal?.addEventListener("click", (event) => { if (event.target === signoutModal) closeSignout(); });
  document.addEventListener("keydown", (event) => { if (event.key === "Escape" && signoutModal && !signoutModal.hidden) closeSignout(); });

  document.querySelectorAll("[data-toggle-password]").forEach((button) => {
    button.addEventListener("click", () => {
      const id = button.getAttribute("data-toggle-password");
      const input = id ? document.getElementById(id) : null;
      if (!input) return;
      const show = input.type === "password";
      input.type = show ? "text" : "password";
      button.textContent = show ? "Hide" : "Show";
    });
  });

  const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  if (!reduce) {
    document.querySelectorAll(".meter-track span").forEach((bar) => {
      const width = bar.style.width;
      if (!width) return;
      bar.style.setProperty("--w", width);
    });
  }
})();
