(() => {
    const form = document.getElementById("book-catalog-filter");
    const results = document.getElementById("book-results");
    if (!form || !results) return;

    const apiUrl = results.dataset.apiUrl;
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";

    function element(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function makeCard(book, isAuthenticated) {
        const column = element("div", "col-6 col-md-4 col-lg-3");
        const card = element("article", "card h-100 book-card shadow-sm");
        const link = element("a", "text-decoration-none");
        link.href = `/Books/Details/${encodeURIComponent(book.id)}`;
        const image = element("img", "card-img-top");
        image.src = book.coverImage || "/images/no-image.png";
        image.alt = book.title;
        image.loading = "lazy";
        link.append(image);

        const body = element("div", "card-body d-flex flex-column");
        const titleLink = element("a", "text-decoration-none");
        titleLink.href = link.href;
        titleLink.append(element("h2", "h6 card-title", book.title));
        const author = element("p", "small text-muted mb-2", book.author);
        const price = element("p", "fw-bold text-primary mt-auto mb-2",
            `${Number(book.salePrice).toLocaleString("vi-VN")} ₫`);
        body.append(titleLink, author);
        if (Number(book.salePrice) < Number(book.price)) {
            const oldPrice = element("del", "small text-muted",
                `${Number(book.price).toLocaleString("vi-VN")} ₫`);
            body.append(oldPrice);
        }
        body.append(price);
        const reviewCount = Number(book.reviewCount) || 0;
        const ratingText = reviewCount > 0
            ? `★ ${Number(book.rating).toFixed(1)} (${reviewCount} lượt đánh giá)`
            : "☆ 0.0 (0 lượt đánh giá)";
        const rating = element("p", "small mb-2", ratingText);
        body.append(rating);

        if (isAuthenticated) {
            const cartForm = element("form", "mt-auto");
            cartForm.method = "post";
            cartForm.action = "/Cart/Add";
            const token = element("input");
            token.type = "hidden";
            token.name = "__RequestVerificationToken";
            token.value = antiForgeryToken;
            const bookId = element("input");
            bookId.type = "hidden";
            bookId.name = "id";
            bookId.value = book.id;
            const returnUrl = element("input");
            returnUrl.type = "hidden";
            returnUrl.name = "returnUrl";
            returnUrl.value = `${location.pathname}${location.search}`;
            const addButton = element("button", "btn btn-sm btn-primary w-100", "Thêm vào giỏ");
            addButton.type = "submit";
            cartForm.append(token, bookId, returnUrl, addButton);
            body.append(cartForm);
        } else {
            const login = element("a", "btn btn-sm btn-outline-primary mt-auto", "Đăng nhập để mua");
            login.href = `/Account/Login?${new URLSearchParams({ returnUrl: `${location.pathname}${location.search}` })}`;
            body.append(login);
        }

        card.append(link, body);
        column.append(card);
        return column;
    }

    function render(data) {
        results.replaceChildren();
        const summary = element("div", "d-flex justify-content-between align-items-center mb-3");
        summary.append(
            element("p", "text-muted mb-0", `Tìm thấy ${data.totalCount} sách`),
            element("span", "text-muted small", `Trang ${data.page} / ${Math.max(1, data.totalPages)}`)
        );
        results.append(summary);

        if (!data.items.length) {
            results.append(element("div", "alert alert-info", "Không tìm thấy sách phù hợp."));
            return;
        }

        const grid = element("div", "row g-4");
        const isAuthenticated = results.dataset.authenticated === "true";
        data.items.forEach(book => grid.append(makeCard(book, isAuthenticated)));
        results.append(grid);

        if (data.totalPages > 1) {
            const nav = element("nav", "mt-4");
            nav.setAttribute("aria-label", "Phân trang sách");
            const list = element("ul", "pagination justify-content-center");
            const params = new URLSearchParams(new FormData(form));
            const addPageLink = (page, label, disabled) => {
                const item = element("li", `page-item${disabled ? " disabled" : ""}`);
                if (disabled) {
                    item.append(element("span", "page-link", label));
                } else {
                    const anchor = element("a", "page-link", label);
                    params.set("page", page);
                    anchor.href = `${location.pathname}?${params.toString()}`;
                    anchor.dataset.page = page;
                    item.append(anchor);
                }
                list.append(item);
            };
            addPageLink(data.page - 1, "Trước", data.page <= 1);
            const current = element("li", "page-item disabled");
            current.append(element("span", "page-link", `${data.page} / ${data.totalPages}`));
            list.append(current);
            addPageLink(data.page + 1, "Sau", data.page >= data.totalPages);
            nav.append(list);
            results.append(nav);
        }
    }

    async function load(params, updateHistory) {
        const targetPage = params.get("page") || new URLSearchParams(location.search).get("page") || "1";
        params.delete("page");
        params.set("page", targetPage);
        const url = `${apiUrl}?${params.toString()}`;
        results.setAttribute("aria-busy", "true");
        try {
            const response = await fetch(url, { headers: { Accept: "application/json" } });
            if (!response.ok) throw new Error(`Catalog request failed (${response.status})`);
            const data = await response.json();
            render(data);
            if (updateHistory) {
                const pageParams = new URLSearchParams(params);
                if (data.page <= 1) pageParams.delete("page");
                history.pushState(null, "", `${location.pathname}?${pageParams.toString()}`);
            }
        } catch (error) {
            console.error(error);
            location.assign(`${form.action}?${params.toString()}`);
        } finally {
            results.removeAttribute("aria-busy");
        }
    }

    form.addEventListener("submit", event => {
        event.preventDefault();
        const params = new URLSearchParams(new FormData(form));
        params.set("page", "1");
        load(params, true);
    });

    results.addEventListener("click", event => {
        const anchor = event.target.closest("a[data-page]");
        if (!anchor) return;
        event.preventDefault();
        const params = new URLSearchParams(new FormData(form));
        params.set("page", anchor.dataset.page);
        load(params, true);
    });

    window.addEventListener("popstate", () => {
        const params = new URLSearchParams(location.search);
        form.reset();
        form.elements.namedItem("q").value = params.get("q") || "";
        form.elements.namedItem("sort").value = params.get("sort") || "new";
        form.elements.namedItem("promotion").checked = params.get("promotion") === "true";
        const selectedCategories = params.getAll("category");
        form.querySelectorAll('input[name="category"]').forEach(category => {
            category.checked = selectedCategories.includes(category.value);
        });
        load(params, false);
    });
})();
