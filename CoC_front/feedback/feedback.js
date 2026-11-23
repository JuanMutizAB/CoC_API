const selectMotivo = document.querySelector("#Motivo");

Object.values(Motivo).forEach(valor => {
    const option = document.createElement("option");
    option.value = valor;
    option.textContent = valor;
    selectMotivo.appendChild(option);
});

