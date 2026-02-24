window.mascaras = {
    _instancias: {},

    aplicarMascaraCPF: (elementId) => {
        const element = document.getElementById(elementId);
        if (!element) return;

        // Remove qualquer listener anterior
        if (window.mascaras._instancias[elementId]) {
            element.removeEventListener('input', window.mascaras._instancias[elementId]);
        }

        const formatar = (e) => {
            let value = e.target.value.replace(/\D/g, '');

            if (value.length > 11) {
                value = value.slice(0, 11);
            }

            // Aplica a máscara progressivamente
            if (value.length <= 3) {
                e.target.value = value;
            } else if (value.length <= 6) {
                e.target.value = value.replace(/(\d{3})(\d+)/, '$1.$2');
            } else if (value.length <= 9) {
                e.target.value = value.replace(/(\d{3})(\d{3})(\d+)/, '$1.$2.$3');
            } else {
                e.target.value = value.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');
            }
        };

        element.addEventListener('input', formatar);

        // Formata o valor inicial se existir
        if (element.value) {
            formatar({ target: element });
        }

        window.mascaras._instancias[elementId] = formatar;
    },

    aplicarMascaraTelefone: (elementId) => {
        const element = document.getElementById(elementId);
        if (!element) return;

        // Remove listener anterior se existir
        if (window.mascaras._instancias[elementId]) {
            element.removeEventListener('input', window.mascaras._instancias[elementId]);
        }

        const formatar = (e) => {
            let value = e.target.value.replace(/\D/g, '');

            if (value.length > 11) {
                value = value.slice(0, 11);
            }

            // Aplica máscara progressiva
            if (value.length <= 2) {
                e.target.value = value;
            } else if (value.length <= 6) {
                e.target.value = value.replace(/(\d{2})(\d+)/, '($1) $2');
            } else if (value.length <= 10) {
                e.target.value = value.replace(/(\d{2})(\d{4})(\d+)/, '($1) $2-$3');
            } else {
                e.target.value = value.replace(/(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
            }
        };

        element.addEventListener('input', formatar);

        // Formata valor inicial se existir
        if (element.value) {
            formatar({ target: element });
        }

        window.mascaras._instancias[elementId] = formatar;
    },

    removerMascara: (elementId) => {
        const element = document.getElementById(elementId);
        const listener = window.mascaras._instancias[elementId];

        if (element && listener) {
            element.removeEventListener('input', listener);
            delete window.mascaras._instancias[elementId];
        }
    }
};