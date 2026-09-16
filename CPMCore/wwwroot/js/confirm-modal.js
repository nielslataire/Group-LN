/** In-systeem vervanger voor het kale browser-confirm() — zie Views/Shared/_ConfirmModal.cshtml
 * voor de bijhorende markup. Werkt globaal (window.confirmDialog), zodat elke pagina die de
 * partial + dit script laadt dezelfde, echte Bootstrap-modal krijgt i.p.v. de OS-dialoog.
 * `body` mag kleine HTML bevatten (bv. <b>) — geen gebruikersinvoer zonder eerst te escapen. */
(function () {
    "use strict";

    window.confirmDialog = function confirmDialog(title, body, confirmLabel) {
        return new Promise(function (resolve) {
            var modalEl = document.getElementById("glConfirmModal");
            var btn = document.getElementById("glConfirmBtn");
            if (!modalEl || !btn || !window.bootstrap || !window.bootstrap.Modal) {
                resolve(window.confirm(title)); // vangnet als de modal-markup of Bootstrap-JS ontbreekt
                return;
            }
            document.getElementById("glConfirmTitle").textContent = title;
            document.getElementById("glConfirmBody").innerHTML = body;
            btn.textContent = confirmLabel;
            var modal = window.bootstrap.Modal.getOrCreateInstance(modalEl);
            var settled = false;
            function cleanup(result) {
                if (settled) return;
                settled = true;
                btn.removeEventListener("click", onConfirm);
                modalEl.removeEventListener("hidden.bs.modal", onHide);
                resolve(result);
            }
            function onConfirm() { modal.hide(); cleanup(true); }
            function onHide() { cleanup(false); }
            btn.addEventListener("click", onConfirm);
            modalEl.addEventListener("hidden.bs.modal", onHide);
            modal.show();
        });
    };
})();
