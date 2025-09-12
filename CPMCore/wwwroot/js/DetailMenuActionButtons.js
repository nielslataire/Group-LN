
    (function () {
        // Alle elementen die links ruimte innemen: Porto sidebar + jouw detailmenu
        function getMenus() {
            const arr = [];
            const left = document.getElementById('sidebar-left'); // uit je layout
            if (left) arr.push(left);
            document.querySelectorAll('.menu-offset').forEach(el => arr.push(el)); // jouw detailmenu
            return arr;
        }

          function updateMenusWidthVar() {
            const total = getMenus().reduce((sum, el) => {
              // offsetWidth => 0 wanneer ingeklapt/verborgen, ~300 wanneer open
              return sum + (el ? (el.offsetWidth || 0) : 0);
            }, 0);
    document.documentElement.style.setProperty('--menus-w', total + 'px');
          }

    // init + resize
    updateMenusWidthVar();
    window.addEventListener('resize', updateMenusWidthVar);

    // Reageer op open/dicht/animaties (breedte- of class-wijzigingen)
    const ros = [], mos = [];
          getMenus().forEach(el => {
            const ro = new ResizeObserver(updateMenusWidthVar);
    ro.observe(el);  ros.push(ro);

    const mo = new MutationObserver(updateMenusWidthVar);
    mo.observe(el, {attributes: true, attributeFilter: ['class','style'] });
    mos.push(mo);
          });

          // Porto zet vaak een class op <html> bij toggles (bv. collapsed) → ook daarop luisteren
        const rootMo = new MutationObserver(updateMenusWidthVar);
        rootMo.observe(document.documentElement, {attributes: true, attributeFilter: ['class'] });
        })();
