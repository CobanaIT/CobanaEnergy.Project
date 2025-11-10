function initAutoCompleteDropdown(options) {
    const {
        inputSelector,
        suggestionSelector,
        loaderSelector,
        url,
        queryParam = "search",
        debounceTime = 300,
        minLength = 1,
        pattern = null, // optional regex
        onSelect = () => { },
        renderItem = item => item, // how to render each suggestion's HTML
        getDisplayValue = item => item, // what to show in the textbox after select
    } = options;

    const $input = $(inputSelector);
    const $suggestions = $(suggestionSelector);
    const $loader = $(loaderSelector);

    let timeout = null;
    let selectedIndex = -1;

    // KEYDOWN: navigation + enter
    $input.on("keydown", function (e) {
        const key = e.key;
        const $items = $suggestions.find("li");

        if (["ArrowDown", "ArrowUp", "Enter", "Escape"].includes(key)) {
            if (key === "Escape") {
                $suggestions.hide();
                selectedIndex = -1;
                return;
            }

            if (!$items.length) return;
            e.preventDefault();

            if (key === "ArrowDown") {
                selectedIndex = (selectedIndex + 1) % $items.length;
            } else if (key === "ArrowUp") {
                selectedIndex = (selectedIndex - 1 + $items.length) % $items.length;
            } else if (key === "Enter") {
                if (selectedIndex >= 0) {
                    const $li = $($items[selectedIndex]);
                    const item = $li.data("item");
                    selectValue(item);
                }
            }

            $items.removeClass("active");
            if (selectedIndex >= 0) {
                $($items[selectedIndex]).addClass("active");
                ensureVisible($items[selectedIndex]);
            }
        }
    });

    // KEYUP: handle input (search)
    $input.on("keyup", function (e) {
        if (["ArrowDown", "ArrowUp", "Enter", "Escape"].includes(e.key)) return;

        const query = $(this).val().trim();
        if (query.length < minLength || (pattern && !pattern.test(query))) {
            $suggestions.hide();
            return;
        }

        clearTimeout(timeout);
        timeout = setTimeout(() => {
            $loader.show();

            $.ajax({
                url,
                type: "GET",
                data: { [queryParam]: query },
                success: function (res) {
                    $loader.hide();
                    $suggestions.empty();
                    selectedIndex = -1;

                    if (res.success && res.Data && res.Data.length > 0) {
                        res.Data.forEach(item => {
                            // allow complex HTML rendering
                            const $li = $(`
                                <li class="list-group-item list-group-item-action">
                                    ${renderItem(item)}
                                </li>
                            `);
                            $li.data("item", item);
                            $suggestions.append($li);
                        });

                        $suggestions.show();

                        // auto-select first item
                        selectedIndex = 0;
                        $suggestions.find("li").eq(0).addClass("active");
                    } else {
                        $suggestions.hide();
                    }
                },
                error: function () {
                    $loader.hide();
                    $suggestions.hide();
                },
            });
        }, debounceTime);
    });

    // CLICK: select item
    $suggestions.on("click", "li", function () {
        const item = $(this).data("item");
        selectValue(item);
    });

    // CLICK OUTSIDE: hide
    $(document).on("click", function (e) {
        if (!$(e.target).closest(inputSelector + ", " + suggestionSelector).length) {
            $suggestions.hide();
            selectedIndex = -1;
        }
    });

    function selectValue(item) {
        const displayValue = getDisplayValue(item);
        $input.val(displayValue);
        $suggestions.hide();
        onSelect(item);
    }

    function ensureVisible(el) {
        const container = $suggestions[0];
        if (!container) return;
        const itemRect = el.getBoundingClientRect();
        const containerRect = container.getBoundingClientRect();
        if (itemRect.bottom > containerRect.bottom)
            container.scrollTop += itemRect.bottom - containerRect.bottom;
        else if (itemRect.top < containerRect.top)
            container.scrollTop -= containerRect.top - itemRect.top;
    }
}