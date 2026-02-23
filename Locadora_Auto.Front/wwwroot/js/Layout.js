

window.themeManager = {

    setTheme: function (isDark) {
        if (isDark) {
            document.body.classList.add("dark-theme");
            document.body.classList.remove("light-theme");
            localStorage.setItem("theme", "dark");
        } else {
            document.body.classList.add("light-theme");
            document.body.classList.remove("dark-theme");
            localStorage.setItem("theme", "light");
        }
    },

    loadTheme: function () {
        const savedTheme = localStorage.getItem("theme");

        if (savedTheme === "dark") {
            document.body.classList.add("dark-theme");
            document.body.classList.remove("light-theme");
            return true;
        } else {
            document.body.classList.add("light-theme");
            document.body.classList.remove("dark-theme");
            return false;
        }
    }
};