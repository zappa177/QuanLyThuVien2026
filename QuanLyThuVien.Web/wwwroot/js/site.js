// ==========================================
// 1. XỬ LÝ NÚT LÊN ĐẦU TRANG (BACK TO TOP)
// ==========================================
let btnBackToTop = document.getElementById("btnBackToTop");

if (btnBackToTop) {
    // Bắt sự kiện khi người dùng cuộn chuột
    window.onscroll = function () {
        if (document.body.scrollTop > 200 || document.documentElement.scrollTop > 200) {
            btnBackToTop.style.display = "block";
        } else {
            btnBackToTop.style.display = "none";
        }
    };

    // Sự kiện khi bấm vào nút
    btnBackToTop.addEventListener("click", function () {
        window.scrollTo({
            top: 0,
            behavior: "smooth" // Hiệu ứng cuộn trượt mượt mà
        });
    });
}

// ==========================================
// 2. XỬ LÝ SIDEBAR TRÊN MOBILE
// ==========================================
function toggleSidebar() {
    var sidebar = document.getElementById("sidebar");
    var overlay = document.getElementById("sidebarOverlay");

    if (sidebar && overlay) {
        sidebar.classList.toggle("active");
        overlay.classList.toggle("active");
    }
}

// ==========================================
// 3. XỬ LÝ THÊM SÁCH VÀO GIỎ TỪ MODAL
// ==========================================
function addToCartFromModal() {
    var bookId = $('#infoBookId').val();
    var quantity = $('#infoQuantity').val(); // Lấy số lượng

    if (quantity < 1) {
        alert("Số lượng mượn tối thiểu là 1");
        return;
    }

    // Đảm bảo gửi kèm parameter quantity
    $.post('/Home/AddToCartDb', { bookId: bookId, quantity: quantity }, function (res) {
        if (res.success) {
            alert("Đã thêm thành công vào giỏ hàng!");

            // Cập nhật lại số lượng badge đỏ trên header
            if (res.newCount !== undefined) {
                let badge = $('#cartItemCountBadge');
                if (badge.length > 0) {
                    badge.text(res.newCount);
                    badge.css('display', res.newCount > 0 ? 'inline-block' : 'none');
                }
            }

            $('#infoBookModal').modal('hide');
        } else {
            alert(res.message);
        }
    }).fail(function () {
        alert("Lỗi kết nối đến máy chủ.");
    });
}