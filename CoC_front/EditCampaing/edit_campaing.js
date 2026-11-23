const publishTimeInput = document.querySelector("#campaign-publish-time");
const campaignForm = document.querySelector("#campaign-form");
const formStatus = document.querySelector("#form-status");

publishTimeInput.addEventListener("input", () => {
    const digits = publishTimeInput.value.replace(/\D/g, "").slice(0, 4);
    publishTimeInput.value = digits.length > 2
        ? `${digits.slice(0, 2)}:${digits.slice(2)}`
        : digits;
});

campaignForm.addEventListener("submit", (event) => {
    event.preventDefault();

    const time = publishTimeInput.value;
    if (!/^([01][0-9]|2[0-3]):[0-5][0-9]$/.test(time)) {
        publishTimeInput.setCustomValidity("Introduza um horário válido entre 00:00 e 23:59.");
        publishTimeInput.reportValidity();
        formStatus.textContent = "";
        return;
    }

    publishTimeInput.setCustomValidity("");

    const campaignData = {
        name: document.querySelector("#campaign-name").value,
        description: document.querySelector("#campaign-description").value,
        text: document.querySelector("#campaign-text").value,
        image: document.querySelector("#campaign-image").files[0]?.name ?? null,
        publishDate: document.querySelector("#campaign-publish-date").value,
        publishTime: time
    };

    console.log("Dados da campanha:", campaignData);
    formStatus.textContent =
        `Dados: ${campaignData.name} | ${campaignData.publishDate} às ${campaignData.publishTime}`;
});

loadCampaign();