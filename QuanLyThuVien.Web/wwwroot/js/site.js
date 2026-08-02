// 1. Mở Modal Giỏ Hàng (Giữ nguyên)
function openCartModal() {
    $('#cartModalContent').html('<div class="text-center py-3">Đang tải...</div>');
    $('#cartModal').modal('show');

    $.get('/Cart/GetCartData', function (res) {
        if (res.success) {
            let html = '';
            if (res.items.length === 0) {
                html = '<div class="text-center text-muted my-4">Giỏ hàng của bạn đang trống.</div>';
                $('#btnCreateTicket').prop('disabled', true);
            } else {
                $('#btnCreateTicket').prop('disabled', false);
                res.items.forEach(item => {
                    html += `
                        <div class="d-flex align-items-center mb-3 p-2 border rounded" id="cart-item-${item.id}">
                            <img src="${item.coverImage}" class="me-3" style="width: 50px; height: 70px; object-fit: contain;">
                            <div class="flex-grow-1">
                                <h6 class="mb-1 fw-bold">${item.title}</h6>
                                <small class="text-muted">Người mượn: <strong>${res.username}</strong></small>
                            </div>
                            <button class="btn btn-sm btn-danger px-3" onclick="removeCartItem(${item.id})">Xóa</button>
                        </div>
                    `;
                });
            }
            $('#cartModalContent').html(html);

            if (res.isLibrarian) {
                $('#librarianInputArea').show();
                $('#targetUsername').val('');
            } else {
                $('#librarianInputArea').hide();
            }
        }
    });
}

// 2. Xóa sách khỏi giỏ (ĐÃ THÊM LẠI TOKEN)
function removeCartItem(cartId) {
    if (confirm("Xóa cuốn sách này khỏi giỏ?")) {
        // Lấy Token bảo mật từ giao diện
        var token = $('input[name="__RequestVerificationToken"]').val();

        if (!token) {
            alert("Lỗi: Không tìm thấy Token bảo mật trên trang!");
            return;
        }

        $.ajax({
            url: '/Cart/RemoveItem',
            type: 'POST',
            data: {
                id: cartId,
                __RequestVerificationToken: token // <--- Nhét khóa vào để qua cửa bảo vệ
            },
            success: function (res) {
                if (res.success) {
                    $('#cart-item-' + cartId).fadeOut(300, function () {
                        $(this).remove();
                        if ($('#cartModalContent').children().length === 0) {
                            $('#cartModalContent').html('<div class="text-center text-muted my-4">Giỏ hàng của bạn đang trống.</div>');
                            $('#btnCreateTicket').prop('disabled', true);
                        }
                    });

                    let currentBadge = parseInt($('#cartItemCountBadge').text()) || 1;
                    $('#cartItemCountBadge').text(currentBadge - 1);
                    if (currentBadge - 1 <= 0) $('#cartItemCountBadge').hide();
                } else {
                    alert(res.message);
                }
            },
            error: function (xhr) {
                alert("Yêu cầu bị từ chối! (Mã lỗi: " + xhr.status + ")");
            }
        });
    }
}

// 3. Tạo phiếu mượn (ĐÃ THÊM LẠI TOKEN)
function createTicketFromCart() {
    var token = $('input[name="__RequestVerificationToken"]').val();
    var targetUser = $('#targetUsername').is(':visible') ? $('#targetUsername').val() : '';

    if (!token) {
        alert("Lỗi: Không tìm thấy Token bảo mật trên trang!");
        return;
    }

    $('#btnCreateTicket').prop('disabled', true).text('Đang xử lý...');

    $.ajax({
        url: '/Cart/CreateTicket',
        type: 'POST',
        data: {
            targetUsername: targetUser,
            __RequestVerificationToken: token // <--- Nhét khóa vào
        },
        success: function (res) {
            if (res.success) {
                alert(res.message);
                $('#cartModal').modal('hide');
                $('#cartItemCountBadge').text('0').hide();

                // Chuyển tới trang Danh sách phiếu mượn để xem
                window.location.href = '/BorrowTickets/Index';
            } else {
                alert(res.message);
                $('#btnCreateTicket').prop('disabled', false).text('Tạo phiếu mượn');
            }
        },
        error: function (xhr) {
            alert("Yêu cầu bị từ chối! (Mã lỗi: " + xhr.status + ")");
            $('#btnCreateTicket').prop('disabled', false).text('Tạo phiếu mượn');
        }
    });
}

///////////////js cho nút lên top hiện khi cuộn trang

// 1. Lấy phần tử nút Lên Top
let btnBackToTop = document.getElementById("btnBackToTop");

// 2. Bắt sự kiện khi người dùng cuộn chuột
window.onscroll = function () {
    scrollFunction();
};

function scrollFunction() {
    // Nếu cuộn xuống quá 200px từ đầu trang thì hiện nút
    if (document.body.scrollTop > 200 || document.documentElement.scrollTop > 200) {
        btnBackToTop.style.display = "block";
    } else {
        btnBackToTop.style.display = "none";
    }
}

// 3. Sự kiện khi bấm vào nút
btnBackToTop.addEventListener("click", function () {
    window.scrollTo({
        top: 0,
        behavior: "smooth" // Hiệu ứng cuộn trượt mượt mà
    });
});

// Hàm điều khiển Sidebar trên Mobile
function toggleSidebar() {
    var sidebar = document.getElementById("sidebar");
    var overlay = document.getElementById("sidebarOverlay");

    if (sidebar && overlay) {
        sidebar.classList.toggle("active");
        overlay.classList.toggle("active");
    }
}