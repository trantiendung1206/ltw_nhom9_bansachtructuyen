document.querySelectorAll("[data-password-toggle]").forEach((button) => {
    button.addEventListener("click", () => {
        const input = button.closest(".password-field")?.querySelector("input");
        if (!input) {
            return;
        }

        const showPassword = input.type === "password";
        input.type = showPassword ? "text" : "password";
        button.setAttribute("aria-label", showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu");
        button.setAttribute("aria-pressed", String(showPassword));
    });
});
