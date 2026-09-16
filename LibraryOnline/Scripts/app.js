"use strict";
const $ = (s) => document.querySelector(s),
  main = $("#main"),
  modal = $("#modal");
let session = { user: null, csrfToken: "" },
  categories = [],
  catalogPage = 1,
  lastBooks = [],
  users = [];
const page = document.body.dataset.page,
  staff = () =>
    session.user && ["Administrator", "Librarian"].includes(session.user.role),
  admin = () => session.user?.role === "Administrator",
  member = () => session.user?.role === "Member";
const esc = (v) =>
  String(v ?? "").replace(
    /[&<>"']/g,
    (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[
        c
      ],
  );
const date = (v) =>
    v ? new Date(v).toLocaleDateString("vi-VN", { timeZone: "UTC" }) : "—",
  money = (v) => Number(v).toLocaleString("vi-VN") + " đ";
const labels = {
  Reserved: "Đã đặt",
  Borrowed: "Đang mượn",
  Returned: "Đã trả",
  Overdue: "Quá hạn",
  Cancelled: "Đã hủy",
  Pending: "Chờ duyệt",
  Approved: "Đã duyệt",
  Rejected: "Từ chối",
  Member: "Thành viên",
  Librarian: "Thủ thư",
  Administrator: "Quản trị viên",
};
const badge = (s) =>
  `<span class="badge ${esc(s)}">${esc(labels[s] || s)}</span>`;
async function api(path, method = "GET", body) {
  const response = await fetch("/api/" + path, {
    method,
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-Token": session.csrfToken,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  let data;
  try {
    data = JSON.parse(text);
  } catch {
    data = { message: "Không thể đọc phản hồi từ máy chủ." };
  }
  if (!response.ok)
    throw Error(
      data.message ||
        data.Message ||
        "Bạn không có quyền thực hiện thao tác này.",
    );
  return data;
}
function toast(text) {
  $("#toast").textContent = text;
  $("#toast").hidden = false;
  setTimeout(() => ($("#toast").hidden = true), 5000);
}
function openModal(html) {
  $("#modal-body").innerHTML = html;
  if (!modal.open) modal.showModal();
}
function title(eyebrow, heading, description, action = "") {
  return `<div class="page-title"><div><div class="eyebrow">${eyebrow}</div><h1>${heading}</h1><p>${description}</p></div>${action}</div>`;
}
function field(label, name, value = "", type = "text", extra = "") {
  return `<label class="field">${label}<input name="${name}" type="${type}" value="${esc(value)}" ${extra}></label>`;
}
function select(label, name, options, value = "") {
  return `<label class="field">${label}<select name="${name}">${options.map((o) => `<option value="${esc(o.value)}" ${String(o.value) === String(value) ? "selected" : ""}>${esc(o.label)}</option>`).join("")}</select></label>`;
}
function table(head, rows) {
  return `<div class="table-wrap"><table><thead><tr>${head.map((h) => `<th>${h}</th>`).join("")}</tr></thead><tbody>${rows.length ? rows.join("") : `<tr><td colspan="${head.length}"><div class="empty">Chưa có dữ liệu để hiển thị.</div></td></tr>`}</tbody></table></div>`;
}
function submitForm(id, handler) {
  const form = document.getElementById(id);
  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const button = form.querySelector("[type=submit]");
    button.disabled = true;
    form.querySelector(".error")?.remove();
    try {
      await handler(Object.fromEntries(new FormData(form)), form);
    } catch (err) {
      form.insertAdjacentHTML(
        "beforeend",
        `<div class="error" role="alert">${esc(err.message)}</div>`,
      );
    } finally {
      button.disabled = false;
    }
  });
}
function nav() {
  $(".brand span:last-child").innerHTML =
    esc(session.libraryName || "Thư viện Mở") + "<small>LIBRARY ONLINE</small>";
  const links = [["Catalog", "⌕", "Khám phá sách"]];
  if (session.user) {
    links.push(
      ["Loans", "▤", staff() ? "Quản lý mượn trả" : "Sách đang mượn"],
      ["Fines", "◷", "Tiền phạt"],
    );
    if (member()) links.push(["Reading", "♡", "Danh sách đọc"]);
    links.push(["Notifications", "♧", "Thông báo"]);
    if (staff())
      links.push(
        ["divider", "", "QUẢN LÝ THƯ VIỆN"],
        ["Dashboard", "▥", "Tổng quan"],
        ["ManageBooks", "▦", "Quản lý sách"],
        ["Users", "♙", "Tài khoản"],
      );
    if (admin()) links.push(["Settings", "⚙", "Thiết lập"]);
  }
  $("#nav").innerHTML = links
    .map(([p, i, t]) =>
      p === "divider"
        ? `<div class="nav-separator">${t}</div>`
        : `<a href="/${p}" class="${page === p ? "active" : ""}"><span class="nav-icon">${i}</span>${t}</a>`,
    )
    .join("");
  const current = links.find((x) => x[0] === page);
  $("#breadcrumb").textContent =
    "Thư viện / " +
    (current?.[2] || (page === "Reader" ? "Đọc sách" : "Tài khoản"));
  $("#login-link").hidden = !!session.user;
  $("#logout").hidden = !session.user;
  $("#profile-link").innerHTML = session.user
    ? `<span class="avatar">${esc(session.user.FullName.charAt(0))}</span><span>${esc(session.user.FullName)}<small>${labels[session.user.role]}</small></span>`
    : "Đăng nhập để bắt đầu ↗";
  $("#profile-link").href = session.user ? "/Profile" : "/Account";
}
function bookCard(b) {
  return `<article class="book-card"><button class="cover-wrap" data-action="detail" data-id="${b.Id}" aria-label="Xem ${esc(b.Title)}"><img src="${esc(b.CoverImage || "/Content/covers/default.svg")}" alt="Bìa ${esc(b.Title)}" loading="lazy"></button><div class="book-category">${esc(b.Category?.Name || "Sách")}</div><h3><a href="#book-${b.Id}" data-action="detail" data-id="${b.Id}">${esc(b.Title)}</a></h3><div class="book-author">${esc(b.Author)}</div><div class="book-foot"><span class="badge ${b.AvailableCopies ? "" : "unavailable"}">${b.AvailableCopies ? "● Còn " + b.AvailableCopies + " bản" : "○ Đã hết bản"}</span><button class="arrow-button" data-action="detail" data-id="${b.Id}" aria-label="Chi tiết">↗</button></div></article>`;
}
async function catalog() {
  main.innerHTML = `<section class="hero"><div class="hero-copy"><div class="eyebrow">MỖI TRANG SÁCH, MỘT HÀNH TRÌNH</div><h1>Cuốn sách tiếp theo,<br/>thế giới mới của bạn.</h1><p>Tìm một câu chuyện để yêu, một ý tưởng để khám phá.<br/>Thư viện luôn có một chỗ dành cho bạn.</p><span class="pill">✦ Khám phá · Đặt sách · Đọc theo cách của bạn</span></div><div class="hero-art" aria-hidden="true"><img src="/Content/covers/5.svg" alt=""><img src="/Content/covers/1.svg" alt=""><img src="/Content/covers/29.svg" alt=""></div></section><form id="search-form" class="search-panel"><div class="search-row"><div class="search-box"><span>⌕</span><input name="q" aria-label="Tìm sách" placeholder="Tìm tên sách, tác giả hoặc ISBN…"></div><button class="primary" type="submit">Tìm sách →</button></div><div class="filters"><select name="genre" aria-label="Thể loại"><option value="">Tất cả thể loại</option>${categories.map((c) => `<option value="${c.Id}">${esc(c.Name)}</option>`).join("")}</select><select name="availability" aria-label="Tình trạng"><option value="">Mọi tình trạng</option value="available">Còn sách</option><option value="unavailable">Hết sách</option></select><input name="author" placeholder="Tác giả" aria-label="Tác giả"><input name="publisher" placeholder="Nhà xuất bản" aria-label="Nhà xuất bản"><button type="reset" class="text-button">Xóa bộ lọc</button></div></form><div class="section-head"><div><h2>Khám phá kho sách</h2><p id="catalog-count"></p></div><small>NHỮNG TRANG SÁCH ĐANG CHỜ BẠN</small></div><div id="book-grid" class="catalog-grid"></div><div id="pagination" class="pagination"></div>`;
  $("#search-form").addEventListener("submit", (e) => {
    e.preventDefault();
    catalogPage = 1;
    loadCatalog().catch((e) => toast(e.message));
  });
  $("#search-form").addEventListener("reset", () =>
    setTimeout(() => {
      catalogPage = 1;
      loadCatalog().catch((e) => toast(e.message));
    }, 0),
  );
  await loadCatalog();
}
async function loadCatalog() {
  const params = new URLSearchParams(new FormData($("#search-form")));
  params.set("page", catalogPage);
  const d = await api("books?" + params);
  $("#catalog-count").textContent =
    d.total + " tựa sách · Chọn một cuốn, bắt đầu hành trình";
  $("#book-grid").innerHTML = d.items.length
    ? d.items.map(bookCard).join("")
    : '<div class="empty">Không tìm thấy sách phù hợp. Thử thay đổi bộ lọc.</div>';
  const total = Math.max(1, Math.ceil(d.total / d.pageSize));
  $("#pagination").innerHTML =
    `<button class="secondary" data-action="prev" ${catalogPage === 1 ? "disabled" : ""}>← Trước</button><span>${catalogPage} / ${total}</span><button class="secondary" data-action="next" ${catalogPage >= total ? "disabled" : ""}>Tiếp →</button>`;
}
async function detail(id) {
  const info = await api(`books/${id}/information`);
  const d = await api("books/" + id),
    b = d.book;
  let lists = [];
  if (member()) lists = await api("reading-lists");
  openModal(
    `<div class="detail"><img src="${esc(b.CoverImage)}" alt="${esc(b.Title)}"><div><div class="eyebrow">${esc(b.Category.Name)}</div><h2>${esc(b.Title)}</h2><p>${esc(b.Author)}</p><dl><dt>Nhà xuất bản</dt><dd>${esc(b.Publisher)}</dd><dt>Năm xuất bản</dt><dd>${b.PublicationYear}</dd><dt>ISBN</dt><dd>${esc(b.ISBN)}</dd><dt>Số bản còn</dt><dd>${b.AvailableCopies} / ${b.TotalCopies}</dd></dl><p>${esc(b.Description)}</p><div class="actions">${member() ? `<button class="primary" data-action="reserve" data-id="${id}" ${!b.AvailableCopies ? "disabled" : ""}>Đặt sách để nhận →</button>` : !session.user ? '<a class="button" href="/Account">Đăng nhập để đặt sách</a>' : ""}</div>${member() ? `<form id="save-book-form">${select("Lưu vào danh sách đọc", "ListId", [{ value: "", label: "Chọn danh sách" }, ...lists.map((l) => ({ value: l.Id, label: l.Name }))])}<button type="submit" class="secondary">♡ Lưu sách</button><a class="pill-link" href="/Reading">Tạo danh sách</a></form>` : ""}</div></div>${informationPanel(id, info)}<div class="section-head"><h3>Cùng thể loại</h3></div>${d.related.map((x) => `<button class="secondary" data-action="detail" data-id="${x.Id}">${esc(x.Title)}</button>`).join(" ")}`,
  );
  if (member())
    submitForm("save-book-form", async (f) => {
      if (!f.ListId) throw Error("Hãy chọn một danh sách.");
      await api(`reading-lists/${f.ListId}/books`, "POST", { BookId: id });
      toast("Đã lưu vào danh sách đọc.");
      modal.close();
    });
}
async function account(register = false) {
  if (session.user) {
    location.href = "/Profile";
    return;
  }
  main.innerHTML = `<div class="auth"><div class="auth-art"><div class="eyebrow">THƯ VIỆN MỞ</div><h2>Một tài khoản.<br/>Hàng ngàn<br/>điều để khám phá.</h2><p>Giữ chỗ cho cuốn sách yêu thích, theo dõi lịch mượn và xây dựng góc đọc của riêng bạn.</p></div><section><h1>${register ? "Tạo tài khoản" : "Chào mừng trở lại."}</h1><p>${register ? "Bắt đầu hành trình đọc sách hôm nay." : "Đăng nhập để tiếp tục hành trình đọc."}</p><div class="tab-buttons"><button data-action="login-tab" class="${!register ? "active" : ""}">Đăng nhập</button><button data-action="register-tab" class="${register ? "active" : ""}">Đăng ký</button></div><form id="auth-form">${register ? field("Họ và tên", "FullName", "", "text", 'required maxlength="100"') : ""}${field("Email", "Email", "", "email", 'required autocomplete="username"')}${field("Mật khẩu", "Password", "", "password", 'required autocomplete="' + (register ? "new-password" : "current-password") + '"')}<button class="primary" type="submit">${register ? "Đăng ký" : "Đăng nhập"} →</button><p class="hint">${register ? "Tối thiểu 10 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt." : "Tài khoản demo: member01@library.local<br>Mật khẩu: do bạn cấu hình khi cài đặt"}</p></form></section></div>`;
  submitForm("auth-form", async (f) => {
    await api("session/" + (register ? "register" : "login"), "POST", f);
    if (register) {
      await account(false);
      toast("Tạo tài khoản thành công. Hãy đăng nhập.");
    } else {
      const back = new URLSearchParams(location.search).get("book");
      location.href =
        back && /^\d+$/.test(back) ? `/Reader?book=${back}` : "/Catalog";
    }
  });
}
async function loans() {
  const [items, renewals] = await Promise.all([api("loans"), api("renewals")]);
  main.innerHTML =
    title(
      "THEO DÕI HÀNH TRÌNH ĐỌC",
      staff() ? "Quản lý mượn trả" : "Sách & lịch sử mượn",
      "Phiếu đặt, ngày đến hạn và các yêu cầu gia hạn tại một nơi.",
      '<a class="button secondary" href="/Reports/Export?kind=loans">↓ Xuất PDF</a>',
    ) +
    `<div class="panel"><div class="section-head"><h3>Phiếu mượn (${items.length})</h3><select id="loan-filter" aria-label="Lọc trạng thái"><option value="">Tất cả trạng thái</option>${["Reserved", "Borrowed", "Overdue", "Returned", "Cancelled"].map((s) => `<option>${s}</option>`).join("")}</select></div><div id="loan-table"></div></div><div class="panel"><h3>Yêu cầu gia hạn</h3>${table(
      ["Phiếu / Sách", "Thành viên", "Ngày gửi", "Kết quả", "Thao tác"],
      renewals.map(
        (r) =>
          `<tr><td>#${r.LoanId} · ${esc(r.title)}</td><td>${esc(r.member)}</td><td>${date(r.RequestedAt)}</td><td>${badge(r.Status)}<small>${esc(r.Note)}</small></td><td>${staff() && r.Status === "Pending" ? `<button class="secondary" data-action="approve" data-id="${r.Id}">Duyệt</button><button class="danger" data-action="reject" data-id="${r.Id}">Từ chối</button>` : "—"}</td></tr>`,
      ),
    )}</div>`;
  function render() {
    const status = $("#loan-filter").value;
    $("#loan-table").innerHTML = table(
      [
        "Phiếu / Sách",
        staff() ? "Thành viên" : "Ngày đặt",
        "Hạn nhận / trả",
        "Trạng thái",
        "Thao tác",
      ],
      items
        .filter((l) => !status || l.Status === status)
        .map(
          (l) =>
            `<tr><td>#${l.Id}<br><strong>${esc(l.title)}</strong></td><td>${staff() ? esc(l.member) : date(l.ReservedAt)}</td><td>${date(l.Status === "Reserved" ? l.PickupExpiresAt : l.DueAt)}<small>${l.Status === "Reserved" ? "Hạn nhận" : "Gia hạn " + l.RenewalCount + " lần"}</small></td><td>${badge(l.Status)}</td><td>${l.Status === "Reserved" ? `${staff() ? `<button class="secondary" data-action="checkout" data-id="${l.Id}">Giao sách</button>` : ""}<button class="danger" data-action="cancel" data-id="${l.Id}">Hủy đặt</button>` : ""}${["Borrowed", "Overdue"].includes(l.Status) && staff() ? `<button class="secondary" data-action="return" data-id="${l.Id}">Nhận trả</button>` : ""}${l.Status === "Borrowed" && member() ? `<a class="button secondary" href="/Reader?book=${l.BookId}">Đọc sách</a>` : ""}${l.Status === "Borrowed" && member() ? `<button class="secondary" data-action="renew" data-id="${l.Id}">Xin gia hạn</button>` : ""}</td></tr>`,
        ),
    );
  }
  $("#loan-filter").addEventListener("change", render);
  render();
}
async function fines() {
  const items = await api("fines");
  main.innerHTML =
    title(
      "MINH BẠCH & DỄ THEO DÕI",
      "Tiền phạt & thanh toán",
      "Phạt được tính theo số ngày quá hạn và mức phạt tại thời điểm nhận sách.",
      '<a class="button secondary" href="/Reports/Export?kind=fines">↓ Xuất PDF</a>',
    ) +
    `<div class="stats"><div class="stat"><small>Tổng tiền phạt</small><strong>${money(items.reduce((s, x) => s + x.Amount, 0))}</strong></div><div class="stat"><small>Đã thanh toán</small><strong>${money(items.reduce((s, x) => s + x.Paid, 0))}</strong></div><div class="stat"><small>Còn phải thanh toán</small><strong>${money(items.reduce((s, x) => s + x.outstanding, 0))}</strong></div></div><div class="panel">${table(
      [
        "Phiếu / Thành viên",
        "Sách",
        "Tiền phạt",
        "Đã trả / Còn nợ",
        "Thanh toán",
      ],
      items.map(
        (f) =>
          `<tr><td>#${f.LoanId}<small>${esc(f.member)}</small></td><td>${esc(f.title)}</td><td>${money(f.Amount)}</td><td>${money(f.Paid)}<small>Còn ${money(f.outstanding)}</small></td><td>${staff() && f.outstanding ? `<button class="secondary" data-action="pay" data-id="${f.Id}" data-max="${f.outstanding}">Ghi nhận</button>` : ""}<details><summary>${f.payments.length} lần thanh toán</summary>${f.payments.map((p) => `<small>${date(p.PaidAt)} · ${money(p.Amount)} · ${esc(p.Note)}</small>`).join("")}</details></td></tr>`,
      ),
    )}</div>`;
}
async function reading() {
  const [lists, recs] = await Promise.all([
    api("reading-lists"),
    api("recommendations"),
  ]);
  main.innerHTML =
    title(
      "GÓC ĐỌC CỦA RIÊNG BẠN",
      "Danh sách đọc",
      "Lưu những cuốn sách muốn đọc và khám phá gợi ý theo thể loại yêu thích.",
      '<button class="primary" data-action="new-list">+ Tạo danh sách</button>',
    ) +
    lists
      .map(
        (l) =>
          `<section class="panel"><div class="section-head"><h2>${esc(l.Name)}</h2><button class="text-button" data-action="delete-list" data-id="${l.Id}">Xóa danh sách</button></div><div class="list-books">${l.items.length ? l.items.map((i) => `<div class="list-book"><a href="#" data-action="detail" data-id="${i.BookId}"><img src="${esc(i.Book.CoverImage)}" alt="${esc(i.Book.Title)}">${esc(i.Book.Title)}</a><button class="text-button" data-action="remove-list-book" data-id="${l.Id}" data-book="${i.BookId}">Bỏ lưu</button></div>`).join("") : "<p>Danh sách còn trống. Mở chi tiết một cuốn sách để lưu.</p>"}</div></section>`,
      )
      .join("") +
    `<div class="section-head"><div><h2>Có thể bạn sẽ thích</h2><p>Dựa trên lịch sử mượn và thể loại trong hồ sơ của bạn.</p></div><a class="pill-link" href="/Profile">Chọn thể loại</a></div><div class="catalog-grid">${recs.map(bookCard).join("")}</div>`;
}
async function notifications() {
  const items = await api("notifications");
  main.innerHTML =
    title(
      "LUÔN CẬP NHẬT",
      "Thông báo của bạn",
      "Xác nhận đặt sách, nhắc hạn trả và các khoản thanh toán.",
    ) +
    `<div class="panel">${items.length ? items.map((n) => `<div class="notification ${n.IsRead ? "" : "unread"}"><div>${esc(n.Message)}<small>${date(n.CreatedAt)}</small></div>${!n.IsRead ? `<button class="text-button" data-action="read" data-id="${n.Id}">Đánh dấu đã đọc</button>` : ""}</div>`).join("") : '<div class="empty">Chưa có thông báo.</div>'}</div>`;
}
function chart(items) {
  const max = Math.max(1, ...items.map((x) => x.count));
  return items.length
    ? items
        .map(
          (x) =>
            `<div class="chart-row"><span>${esc(x.name)}</span><div class="bar-track"><div class="bar" data-percent="${(x.count / max) * 100}"></div></div><strong>${x.count}</strong></div>`,
        )
        .join("")
    : "<p>Chưa có dữ liệu.</p>";
}
async function dashboard() {
  const d = await api("dashboard");
  main.innerHTML =
    title(
      "NHỊP ĐỌC CỦA THƯ VIỆN",
      "Tổng quan hoạt động",
      "Số liệu cập nhật từ các giao dịch mượn, trả và thanh toán thực tế.",
    ) +
    `<div class="stats">${[
      [d.books, "Tựa sách", d.copies + " bản · " + d.available + " sẵn sàng"],
      [d.active, "Phiếu đang mượn", d.overdue + " phiếu quá hạn"],
      [d.members, "Thành viên", d.activeMembers + " thành viên từng mượn"],
      [
        d.overdueRate + "%",
        "Tỷ lệ đang quá hạn",
        "Trên tổng phiếu đã nhận sách",
      ],
    ]
      .map(
        (x) =>
          `<div class="stat"><small>${x[1]}</small><strong>${x[0]}</strong><p>${x[2]}</p></div>`,
      )
      .join(
        "",
      )}</div><div class="two-cols"><section class="panel"><h3>Sách được mượn nhiều</h3>${chart(d.popularBooks)}</section><section class="panel"><h3>Thể loại được yêu thích</h3>${chart(d.genres)}</section><section class="panel"><h3>Lượt mượn theo tháng</h3>${chart(d.monthly)}</section><section class="panel"><h3>Thu phí & công nợ</h3><div class="stat"><small>Tiền phạt đã thu</small><strong>${money(d.fineCollected)}</strong></div><div class="stat"><small>Tiền phạt còn nợ</small><strong>${money(d.outstanding)}</strong></div></section></div>`;
  document
    .querySelectorAll("[data-percent]")
    .forEach((x) => (x.style.width = x.dataset.percent + "%"));
}
async function manageBooks() {
  const d = await api("books?manage=true&pageSize=100");
  lastBooks = d.items;
  main.innerHTML =
    title(
      "KHO SÁCH",
      "Quản lý danh mục",
      "Thêm, chỉnh sửa và lưu trữ sách. Số bản sẵn sàng được quản lý tự động.",
      '<button class="primary" data-action="new-book">+ Thêm sách</button>',
    ) +
    `<div class="panel">${table(
      [
        "Sách / ISBN",
        "Tác giả",
        "Thể loại",
        "Số bản",
        "Trạng thái",
        "Thao tác",
      ],
      d.items.map(
        (b) =>
          `<tr><td><strong>${esc(b.Title)}</strong><small>${esc(b.ISBN)}</small></td><td>${esc(b.Author)}</td><td>${esc(b.Category.Name)}</td><td>${b.AvailableCopies}/${b.TotalCopies}</td><td>${b.IsActive ? "Đang lưu hành" : "Lưu trữ"}</td><td><button class="secondary" data-action="edit-book" data-id="${b.Id}">Sửa</button><button class="secondary" data-action="edit-material" data-id="${b.Id}">Nội dung & giới thiệu</button><button class="danger" data-action="delete-book" data-id="${b.Id}">Xóa / lưu trữ</button></td></tr>`,
      ),
    )}</div>`;
}
async function editBook(id) {
  const b = id ? (await api("books/" + id)).book : {};
  openModal(
    `<h2>${id ? "Chỉnh sửa sách" : "Thêm sách mới"}</h2><p class="hint">Số bản còn tự tính theo số bản đang mượn và đã đặt.</p><form id="book-form"><div class="form-grid">${field("Tên sách", "Title", b.Title, "text", 'required maxlength="200"')}${field("Tác giả", "Author", b.Author, "text", 'required maxlength="150"')}${field("Nhà xuất bản", "Publisher", b.Publisher, "text", 'required maxlength="150"')}${field("Năm xuất bản", "PublicationYear", b.PublicationYear || 2026, "number", 'required min="1000" max="2100"')}${field("ISBN (10 hoặc 13 ký tự)", "ISBN", b.ISBN, "text", 'required pattern="([0-9]{13}|[0-9]{9}[0-9Xx])"')}${select(
      "Thể loại",
      "CategoryId",
      categories.map((c) => ({ value: c.Id, label: c.Name })),
      b.CategoryId,
    )}${field("Tổng số bản", "TotalCopies", b.TotalCopies || 1, "number", 'required min="1" max="10000"')}${select(
      "Trạng thái",
      "IsActive",
      [
        { value: "true", label: "Đang lưu hành" },
        { value: "false", label: "Lưu trữ" },
      ],
      String(b.IsActive ?? true),
    )}${field("Ảnh bìa (URL HTTPS hoặc ảnh có sẵn)", "CoverImage", b.CoverImage || "/Content/covers/default.svg", "text", 'maxlength="1000"')}<label class="field wide">Mô tả<textarea name="Description" required maxlength="5000">${esc(b.Description)}</textarea></label></div><button class="primary" type="submit">Lưu sách</button></form>`,
  );
  submitForm("book-form", async (f) => {
    ["PublicationYear", "CategoryId", "TotalCopies"].forEach(
      (k) => (f[k] = Number(f[k])),
    );
    f.IsActive = f.IsActive === "true";
    f.Version = b.RowVersion;
    await api("books" + (id ? "/" + id : ""), id ? "PUT" : "POST", f);
    modal.close();
    toast("Đã lưu sách.");
    await manageBooks();
  });
}
async function userPage() {
  users = await api("users");
  main.innerHTML =
    title(
      "CỘNG ĐỒNG ĐỌC SÁCH",
      "Quản lý tài khoản",
      "Theo dõi thành viên, trạng thái hoạt động và phân quyền.",
      admin()
        ? '<button class="primary" data-action="new-user">+ Tạo tài khoản</button>'
        : "",
    ) +
    `<div class="panel">${table(
      [
        "Họ và tên",
        "Email",
        "Vai trò",
        "Phiếu đang hoạt động",
        "Trạng thái",
        "Thao tác",
      ],
      users.map(
        (u) =>
          `<tr><td>${esc(u.FullName)}</td><td>${esc(u.Email)}</td><td>${labels[u.role]}</td><td>${u.activeLoans}</td><td>${u.IsActive ? "Hoạt động" : "Đã khóa"}</td><td>${admin() || u.role === "Member" ? `<button class="secondary" data-action="edit-user" data-key="${esc(u.Id)}">Chỉnh sửa</button>` : "—"}</td></tr>`,
      ),
    )}</div>`;
}
function editUser(key) {
  const u = users.find((u) => u.Id === key);
  openModal(
    `<h2>${u ? "Cập nhật tài khoản" : "Tạo tài khoản"}</h2><form id="user-form">${u ? "<p>" + esc(u.FullName) + " · " + esc(u.Email) + "</p>" : field("Họ tên", "FullName", "", "text", "required") + field("Email", "Email", "", "email", "required") + field("Mật khẩu", "Password", "", "password", 'required minlength="10"')}${select(
      "Vai trò",
      "Role",
      (admin() ? ["Member", "Librarian", "Administrator"] : ["Member"]).map(
        (r) => ({ value: r, label: labels[r] }),
      ),
      u?.role || "Member",
    )}${
      u
        ? select(
            "Trạng thái",
            "IsActive",
            [
              { value: "true", label: "Hoạt động" },
              { value: "false", label: "Khóa tài khoản" },
            ],
            String(u.IsActive),
          )
        : ""
    }<button type="submit" class="primary">Lưu tài khoản</button></form>`,
  );
  submitForm("user-form", async (f) => {
    if (u) f.IsActive = f.IsActive === "true";
    await api("users" + (u ? "/" + u.Id : ""), u ? "PUT" : "POST", f);
    modal.close();
    toast("Đã lưu tài khoản.");
    await userPage();
  });
}
async function settings() {
  const p = await api("policy");
  main.innerHTML =
    title(
      "QUẢN TRỊ",
      "Chính sách thư viện",
      "Thay đổi thời hạn mượn, hạn mức và mức phạt. Mức phạt mới áp dụng cho lần nhận sách tiếp theo.",
    ) +
    `<div class="two-cols"><section class="panel"><h3>Quy định mượn sách</h3><form id="policy-form">${field("Tên thư viện", "LibraryName", p.LibraryName, "text", 'required maxlength="100"')}<div class="form-grid">${[
      ["MaxActiveLoans", "Số phiếu tối đa", 1, 30],
      ["LoanDays", "Số ngày mượn", 1, 180],
      ["PickupDays", "Số ngày giữ đặt sách", 1, 14],
      ["RenewalDays", "Số ngày mỗi lần gia hạn", 1, 90],
      ["MaxRenewals", "Số lần gia hạn tối đa", 0, 10],
      ["FinePerDay", "Phạt mỗi ngày (VND)", 0, 1000000],
    ]
      .map(([k, t, min, max]) =>
        field(t, k, p[k], "number", `required min="${min}" max="${max}"`),
      )
      .join(
        "",
      )}</div><button type="submit" class="primary">Lưu chính sách</button></form></section><section class="panel"><div class="section-head"><h3>Thể loại sách</h3><button class="secondary" data-action="new-category">+ Thêm</button></div>${table(
      ["Tên", "Thao tác"],
      categories.map(
        (c) =>
          `<tr><td>${esc(c.Name)}</td><td><button class="text-button" data-action="edit-category" data-id="${c.Id}">Sửa</button><button class="text-button" data-action="delete-category" data-id="${c.Id}">Xóa</button></td></tr>`,
      ),
    )}</section></div>`;
  submitForm("policy-form", async (f) => {
    Object.keys(f)
      .filter((k) => k !== "LibraryName")
      .forEach((k) => (f[k] = Number(f[k])));
    await api("policy", "PUT", f);
    toast("Đã cập nhật chính sách.");
  });
}
async function profile() {
  const u = session.user;
  main.innerHTML =
    title(
      "TÀI KHOẢN CÁ NHÂN",
      "Hồ sơ của bạn",
      "Cập nhật thông tin và chọn thể loại yêu thích để nhận gợi ý phù hợp.",
    ) +
    `<div class="two-cols"><section class="panel"><h3>Thông tin cá nhân</h3><p class="hint">${esc(u.Email)} · ${labels[u.role]}</p><form id="profile-form">${field("Họ và tên", "FullName", u.FullName, "text", 'required maxlength="100"')}${field("Điện thoại", "PhoneNumber", u.PhoneNumber, "tel", 'maxlength="30"')}<p class="hint">Thể loại yêu thích</p><div class="checkboxes">${categories.map((c) => `<label><input type="checkbox" name="genre" value="${c.Id}" ${u.genres.includes(c.Id) ? "checked" : ""}> ${esc(c.Name)}</label>`).join("")}</div><p><button type="submit" class="primary">Lưu hồ sơ</button></p></form></section><section class="panel"><h3>Đổi mật khẩu</h3><form id="password-form">${field("Mật khẩu hiện tại", "OldPassword", "", "password", 'required autocomplete="current-password"')}${field("Mật khẩu mới", "NewPassword", "", "password", 'required minlength="10" autocomplete="new-password"')}<p class="hint">Tối thiểu 10 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Bạn sẽ đăng nhập lại sau khi đổi.</p><button type="submit" class="primary">Đổi mật khẩu</button></form></section></div>`;
  submitForm("profile-form", async (f, form) => {
    f.Genres = [...form.querySelectorAll("[name=genre]:checked")].map((x) =>
      Number(x.value),
    );
    delete f.genre;
    await api("session/profile", "PUT", f);
    session = await api("session");
    nav();
    toast("Đã lưu hồ sơ.");
  });
  submitForm("password-form", async (f) => {
    await api("session/password", "POST", f);
    location.href = "/Account";
  });
}
function nameModal(heading, value, handler) {
  openModal(
    `<h2>${heading}</h2><form id="name-form">${field("Tên", "Name", value, "text", 'required maxlength="80"')}<button type="submit" class="primary">Lưu</button></form>`,
  );
  submitForm("name-form", async (f) => {
    await handler(f);
    modal.close();
    toast("Đã lưu.");
  });
}

function prose(text) {
  return String(text || "")
    .split(/\n\s*\n/)
    .filter(Boolean)
    .map((p) => `<p>${esc(p).replace(/\n/g, "<br>")}</p>`)
    .join("");
}
function informationPanel(id, info) {
  return `<section class="book-information"><div class="section-head"><h3>Đọc & khám phá tác phẩm</h3></div>
  ${info.isDemo ? '<p class="demo-notice">Bản đọc minh họa, không phải nguyên văn tác phẩm.</p>' : ""}
  <div class="actions">${info.hasPreview ? `<a class="button secondary" href="/Reader?book=${id}&preview=1">Xem trước nội dung</a>` : '<span class="hint">Chưa có đoạn đọc thử.</span>'}
  ${info.canRead ? `<a class="button" href="/Reader?book=${id}">Đọc sách ngay →</a>` : info.hasDigitalContent ? `<span class="hint">Đọc toàn bộ sau khi nhận sách, trong thời hạn mượn.</span>` : '<span class="hint">Chưa có bản đọc điện tử.</span>'}</div>
  <details open><summary>Về tác phẩm</summary><div class="information-prose">${prose(info.workIntroduction || "Chưa có giới thiệu tác phẩm.")}</div></details>
  <details><summary>Tác giả · ${esc(info.author)}</summary><div class="information-prose">${prose(info.authorBiography || "Chưa có tiểu sử tác giả.")}</div></details>
  <details><summary>Nhà xuất bản · ${esc(info.publisher)}</summary><div class="information-prose">${prose(info.publisherInformation || "Chưa có thông tin giới thiệu nhà xuất bản.")}</div></details></section>`;
}
async function editMaterial(id) {
  const [m, d] = await Promise.all([
    api(`books/${id}/material`),
    api(`books/${id}`),
  ]);
  const area = (label, name, value, max, rows = 4) =>
    `<label class="field wide">${label}<textarea name="${name}" maxlength="${max}" rows="${rows}">${esc(value)}</textarea></label>`;
  openModal(`<h2>Nội dung & giới thiệu</h2><p>${esc(d.book.Title)}</p><form id="material-form">
    ${area("Thông tin tác giả / tiểu sử / nguồn tham khảo", "AuthorBiography", m.AuthorBiography, 6000)}
    ${area("Giới thiệu tác phẩm", "WorkIntroduction", m.WorkIntroduction, 6000)}
    ${area("Nhà xuất bản / giới thiệu / liên hệ", "PublisherInformation", m.PublisherInformation, 6000)}
    ${area("Đoạn xem trước công khai (tối đa 6.000 ký tự)", "PreviewText", m.PreviewText, 6000, 6)}
    ${area("Nội dung đọc (văn bản thuần, tối đa 500.000 ký tự)", "FullText", m.FullText, 500000, 14)}
    <p class="hint">Mỗi chương bắt đầu bằng một dòng tiêu đề. Ngăn cách các chương bằng một dòng chỉ chứa <strong>---</strong>. Chỉ nhập nội dung bạn được phép phân phối. Đoạn xem trước được xuất bản công khai, độc lập với toàn văn.</p>
    ${select(
      "Xuất bản bản đọc & đoạn xem trước",
      "IsPublished",
      [
        { value: "false", label: "Bản nháp / tắt đọc" },
        { value: "true", label: "Xuất bản" },
      ],
      String(m.IsPublished),
    )}
    ${select(
      "Loại nội dung",
      "IsDemo",
      [
        { value: "true", label: "Văn bản minh họa, không phải nguyên tác" },
        { value: "false", label: "Nội dung tác phẩm được phép phân phối" },
      ],
      String(m.IsDemo),
    )}
    <button type="submit" class="primary">Lưu nội dung</button></form>`);
  submitForm("material-form", async (f) => {
    f.IsPublished = f.IsPublished === "true";
    f.IsDemo = f.IsDemo === "true";
    f.Version = m.Version;
    await api(`books/${id}/material`, "PUT", f);
    modal.close();
    toast("Đã lưu nội dung và thông tin sách.");
  });
}
let readerCheckTimer;
async function reader() {
  clearInterval(readerCheckTimer);
  const params = new URLSearchParams(location.search),
    id = Number(params.get("book")),
    preview = params.get("preview") === "1";
  if (!Number.isInteger(id) || id < 1) {
    main.innerHTML =
      '<div class="empty">Hãy chọn một sách từ kho sách hoặc phiếu mượn.</div>';
    return;
  }
  main.innerHTML = '<div class="loading">Đang mở trang sách…</div>';
  let data,
    chapter = 1;
  try {
    data = await api(`books/${id}/${preview ? "preview" : "reader?chapter=1"}`);
  } catch (e) {
    readerError(e.message, id);
    return;
  }
  const render = () => {
    main.innerHTML = `<section class="reader-page"><a class="pill-link" href="/Catalog">← Về kho sách</a>
    ${title(preview ? "MỘT PHẦN NỘI DUNG" : "KHÔNG GIAN ĐỌC SÁCH", esc(data.title), esc(data.author))}
    ${data.isDemo ? '<p class="demo-notice">Nội dung minh họa do hệ thống tạo, không phải nguyên văn tác phẩm. Thủ thư có thể thay bằng nội dung được phép phân phối.</p>' : ""}
    <div class="reader-toolbar"><span>${preview ? "Bạn đang xem đoạn đọc thử." : data.staffAccess ? "Chế độ kiểm tra nội dung của thủ thư." : "Quyền đọc đến hết ngày " + date(data.dueAt) + " (UTC)."}</span>
    <div class="actions"><label>Cỡ chữ <select id="reader-size"><option value="18">Vừa</option><option value="21" selected>Lớn</option><option value="24">Rất lớn</option></select></label><label>Nền <select id="reader-theme"><option value="paper">Sáng</option><option value="sepia">Giấy ngà</option><option value="night">Tối</option></select></label></div></div>
    ${!preview ? `<nav class="reader-pagination" aria-label="Điều hướng chương"><button id="reader-prev" class="secondary" ${chapter === 1 ? "disabled" : ""}>← Chương trước</button><label>Chương <select id="reader-chapter">${data.chapters.map((c) => `<option value="${c.number}" ${chapter === c.number ? "selected" : ""}>${esc(c.title)}</option>`).join("")}</select></label><button id="reader-next" class="secondary" ${chapter === data.totalChapters ? "disabled" : ""}>Chương tiếp →</button></nav>` : ""}
    <article id="reader-text" class="reader-paper" tabindex="0" aria-label="Nội dung sách">${prose(data.text)}</article>
    ${preview ? `<div class="panel"><h3>Bạn vừa xem hết đoạn đọc thử</h3><p>Nhận sách tại thư viện để được đọc toàn bộ trong thời hạn mượn.</p><a class="button" href="${session.user ? "/Reader?book=" + id : "/Account?book=" + id}">${session.user ? "Mở bản đọc đầy đủ" : "Đăng nhập để đọc"}</a></div>` : `<p class="hint">Chương ${chapter} / ${data.totalChapters} · Quyền đọc được kiểm tra lại khi chuyển chương và định kỳ.</p>`}</section>`;
    $("#reader-size").onchange = (e) =>
      ($("#reader-text").style.fontSize = e.target.value + "px");
    $("#reader-theme").onchange = (e) =>
      ($("#reader-text").className = "reader-paper " + e.target.value);
    if (!preview) {
      $("#reader-prev").onclick = () => change(chapter - 1);
      $("#reader-next").onclick = () => change(chapter + 1);
      $("#reader-chapter").onchange = (e) => change(Number(e.target.value));
    }
  };
  const change = async (n) => {
    try {
      data = await api(`books/${id}/reader?chapter=${n}`);
      chapter = n;
      render();
      $("#reader-text").scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (e) {
      clearInterval(readerCheckTimer);
      readerError(e.message, id);
    }
  };
  render();
  if (!preview)
    readerCheckTimer = setInterval(async () => {
      try {
        await api(`books/${id}/reader?chapter=${chapter}`);
      } catch (e) {
        clearInterval(readerCheckTimer);
        readerError(e.message, id);
      }
    }, 60000);
}
function readerError(message, id) {
  main.innerHTML = `<section class="panel"><h1>Chưa thể mở bản đọc</h1><p>${esc(message)}</p><div class="actions"><a class="button secondary" href="/Reader?book=${id}&preview=1">Xem trước nội dung</a>${!session.user ? `<a class="button" href="/Account?book=${id}">Đăng nhập</a>` : '<a class="button" href="/Loans">Kiểm tra phiếu mượn</a>'}<a class="pill-link" href="/Catalog">Về kho sách</a></div></section>`;
}

const pages = {
  Reader: reader,
  Catalog: catalog,
  Account: account,
  Loans: loans,
  Fines: fines,
  Reading: reading,
  Notifications: notifications,
  Dashboard: dashboard,
  ManageBooks: manageBooks,
  Users: userPage,
  Settings: settings,
  Profile: profile,
};
document.addEventListener("click", async (e) => {
  const b = e.target.closest("[data-action]");
  if (!b) return;
  e.preventDefault();
  const a = b.dataset.action,
    id = Number(b.dataset.id);
  b.disabled = true;
  try {
    if (a === "detail") await detail(id);
    if (a === "edit-material") await editMaterial(id);
    if (a === "prev" || a === "next") {
      catalogPage += a === "prev" ? -1 : 1;
      await loadCatalog();
    }
    if (a === "login-tab" || a === "register-tab")
      await account(a === "register-tab");
    if (a === "reserve") {
      await api("loans", "POST", { BookId: id });
      modal.close();
      toast("Đặt sách thành công. Xem hạn nhận trong Sách đang mượn.");
      if (page === "Catalog") await loadCatalog();
    }
    if (["checkout", "return", "cancel"].includes(a)) {
      if (a === "cancel" && !confirm("Hủy yêu cầu đặt sách này?")) return;
      await api(`loans/${id}/actions`, "POST", { Action: a });
      toast("Đã cập nhật phiếu mượn.");
      await loans();
    }
    if (a === "renew") {
      await api(`loans/${id}/renewals`, "POST", {});
      toast("Đã gửi yêu cầu gia hạn.");
      await loans();
    }
    if (a === "approve" || a === "reject") {
      openModal(
        `<h2>${a === "approve" ? "Duyệt" : "Từ chối"} gia hạn</h2><form id="decision-form">${field("Ghi chú", "Note", "", "text", 'maxlength="400"')}<button class="primary" type="submit">Xác nhận</button></form>`,
      );
      submitForm("decision-form", async (f) => {
        await api(`renewals/${id}/decision`, "POST", {
          Approve: a === "approve",
          Note: f.Note,
        });
        modal.close();
        await loans();
      });
    }
    if (a === "pay") {
      openModal(
        `<h2>Ghi nhận thanh toán</h2><p>Còn nợ: ${money(b.dataset.max)}</p><form id="payment-form">${field("Số tiền đã nhận (VND)", "Amount", b.dataset.max, "number", `required min="1" max="${b.dataset.max}" step="1"`)}${field("Ghi chú / Biên nhận", "Note", "", "text", 'maxlength="200"')}<button type="submit" class="primary">Ghi nhận đã nhận tiền</button></form>`,
      );
      submitForm("payment-form", async (f) => {
        f.Amount = Number(f.Amount);
        await api(`fines/${id}/payments`, "POST", f);
        modal.close();
        toast("Đã ghi nhận thanh toán.");
        await fines();
      });
    }
    if (a === "new-list")
      nameModal("Tạo danh sách đọc", "", async (f) => {
        await api("reading-lists", "POST", f);
        await reading();
      });
    if (a === "delete-list" && confirm("Xóa danh sách này?")) {
      await api("reading-lists/" + id, "DELETE");
      await reading();
    }
    if (a === "remove-list-book") {
      await api(`reading-lists/${id}/books/${b.dataset.book}`, "DELETE");
      await reading();
    }
    if (a === "read") {
      await api(`notifications/${id}/read`, "POST", {});
      await notifications();
      await updateNotifications();
    }
    if (a === "new-book" || a === "edit-book") await editBook(id || null);
    if (
      a === "delete-book" &&
      confirm("Xóa sách? Sách đã có lịch sử sẽ được lưu trữ.")
    ) {
      const r = await api("books/" + id, "DELETE");
      toast(r.message);
      await manageBooks();
    }
    if (a === "new-user" || a === "edit-user") editUser(b.dataset.key);
    if (a === "new-category" || a === "edit-category")
      nameModal(
        a === "new-category" ? "Thêm thể loại" : "Đổi tên thể loại",
        categories.find((c) => c.Id === id)?.Name || "",
        async (f) => {
          await api(
            "categories" + (id ? "/" + id : ""),
            id ? "PUT" : "POST",
            f,
          );
          categories = await api("categories");
          await settings();
        },
      );
    if (a === "delete-category" && confirm("Xóa thể loại này?")) {
      await api("categories/" + id, "DELETE");
      categories = await api("categories");
      await settings();
    }
  } catch (err) {
    toast(err.message);
  } finally {
    b.disabled = false;
  }
});
async function updateNotifications() {
  if (session.user) {
    const n = await api("notifications");
    $("#notification-count").textContent =
      n.filter((x) => !x.IsRead).length || "";
  }
}
$("#close-modal").onclick = () => modal.close();
$("#menu-toggle").onclick = () => $(".sidebar").classList.toggle("open");
$("#logout").onclick = async () => {
  try {
    await api("session/logout", "POST", {});
    location.href = "/Account";
  } catch (e) {
    toast(e.message);
  }
};
document.addEventListener(
  "error",
  (e) => {
    if (e.target.tagName === "IMG" && !e.target.src.endsWith("/default.svg"))
      e.target.src = "/Content/covers/default.svg";
  },
  true,
);
(async () => {
  try {
    session = await api("session");
    categories = await api("categories");
    nav();
    if (!session.user && !["Catalog", "Account", "Reader"].includes(page)) {
      location.href = "/Account";
      return;
    }
    await (pages[page] || catalog)();
    await updateNotifications();
  } catch (e) {
    main.innerHTML = `<div class="empty"><h2>Chưa thể mở trang</h2><p>${esc(e.message)}</p><a class="button secondary" href="/Catalog">Về kho sách</a></div>`;
  }
})();
