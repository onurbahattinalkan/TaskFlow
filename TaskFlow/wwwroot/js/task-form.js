// Görev formunun davranışları.
// root: formun bulunduğu kapsayıcı (Yeni Görev formu ya da Düzenle pop-up'ı).
// Aynı sayfada iki form olduğu için her şeyi root'un İÇİNDE arıyoruz.
function initTaskForm(root) {
    const checks = root.querySelectorAll('.assignee-check');
    const inputs = root.querySelectorAll('.deadline-input');
    const emptyMsg = root.querySelector('.deadline-empty');
    const rowOf = i => root.querySelector(`.deadline-row[data-index="${i}"]`);

    // Seçili kişilerin deadline satırlarını göster, diğerlerini gizle
    function refreshRows() {
        let anySelected = false;
        checks.forEach(c => {
            rowOf(c.dataset.index).classList.toggle('d-none', !c.checked);
            if (c.checked) anySelected = true;
        });
        emptyMsg.classList.toggle('d-none', anySelected);
    }
    checks.forEach(c => c.addEventListener('change', refreshRows));

    // İlk deadline diğerlerine SADECE BİR KEZ kopyalanır.
    // Formda zaten tarih varsa (düzenleme ya da hatalı form) kopyalama yapılmaz.
    let autoFilled = [...inputs].some(i => i.value);

    inputs.forEach(input => {
        input.addEventListener('change', () => {
            if (autoFilled || !input.value) return;

            // Seçili diğer kişilerin BOŞ deadline kutularını doldur
            checks.forEach(c => {
                if (!c.checked) return;
                const other = rowOf(c.dataset.index).querySelector('.deadline-input');
                if (other !== input && !other.value) other.value = input.value;
            });
            autoFilled = true;
        });
    });

    // Yeni kategori adı yazılınca açılır listeyi pasifleştir
    const newCat = root.querySelector('.new-category');
    const catSelect = root.querySelector('.category-select');
    const syncCategory = () => { catSelect.disabled = newCat.value.trim() !== ''; };
    newCat.addEventListener('input', syncCategory);

    refreshRows();
    syncCategory();
}