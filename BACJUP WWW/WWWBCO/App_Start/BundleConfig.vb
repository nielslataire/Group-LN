Imports System.Web
Imports System.Web.Optimization

Public Class BundleConfig
    ' For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
    Public Shared Sub RegisterBundles(ByVal bundles As BundleCollection)
        bundles.Add(New ScriptBundle("~/Vendor/jquerybundle").Include(
                   "~/Vendor/jquery/jquery.js"))

        bundles.Add(New ScriptBundle("~/bundles/jqueryui").Include(
                    "~/Scripts/jquery-ui-{version}.js"))

        bundles.Add(New ScriptBundle("~/bundles/jqueryval").Include(
                    "~/Scripts/jquery.unobtrusive*",
                    "~/Scripts/jquery.validate*"))

        bundles.Add(New ScriptBundle("~/Vendor/headlibs").Include(
                    "~/vendor/modernizr/modernizr.min.js"))


        ' Use the development version of Modernizr to develop with and learn from. Then, when you're
        ' ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
        bundles.Add(New ScriptBundle("~/bundles/modernizr").Include(
                    "~/Scripts/modernizr-*"))

        bundles.Add(New ScriptBundle("~/Vendor/jsbundle").Include(
                    "~/vendor/jquery/jquery.min.js",
                    "~/vendor/jquery.appear/jquery.appear.min.js",
                    "~/vendor/jquery.easing/jquery.easing.min.js",
                    "~/vendor/jquery-cookie/jquery-cookie.min.js",
                    "~/vendor/bootstrap/js/bootstrap.min.js",
                    "~/vendor/common/common.min.js",
                    "~/vendor/jquery.validation/jquery.validation.min.js",
                    "~/vendor/jquery.stellar/jquery.stellar.min.js",
                    "~/vendor/jquery.easy-pie-chart/jquery.easy-pie-chart.min.js",
                    "~/vendor/jquery.gmap/jquery.gmap.min.js",
                    "~/vendor/jquery.lazyload/jquery.lazyload.min.js",
                    "~/vendor/isotope/jquery.isotope.min.js",
                    "~/vendor/owl.carousel/owl.carousel.min.js",
                    "~/vendor/magnific-popup/jquery.magnific-popup.min.js",
                    "~/vendor/rs-plugin/js/jquery.themepunch.tools.min.js",
                    "~/vendor/rs-plugin/js/jquery.themepunch.revolution.min.js",
                    "~/vendor/circle-flip-slideshow/js/jquery.flipshow.min.js",
                    "~/vendor/pnotify/pnotify.custom.js",
                    "~/vendor/vide/vide.min.js"))

        bundles.Add(New ScriptBundle("~/Scripts/jsbundle").Include(
                    "~/Scripts/theme.js",
                    "~/Scripts/custom.js",
                    "~/scripts/views/view.home.js",
                    "~/scripts/real-estate.js",
                    "~/Scripts/theme.init.js"
                    ))

        bundles.Add(New StyleBundle("~/Content/css").Include("~/Content/site.css"))

        bundles.Add(New StyleBundle("~/Content/themes/base/css").Include(
                    "~/Content/themes/base/jquery.ui.core.css",
                    "~/Content/themes/base/jquery.ui.resizable.css",
                    "~/Content/themes/base/jquery.ui.selectable.css",
                    "~/Content/themes/base/jquery.ui.accordion.css",
                    "~/Content/themes/base/jquery.ui.autocomplete.css",
                    "~/Content/themes/base/jquery.ui.button.css",
                    "~/Content/themes/base/jquery.ui.dialog.css",
                    "~/Content/themes/base/jquery.ui.slider.css",
                    "~/Content/themes/base/jquery.ui.tabs.css",
                    "~/Content/themes/base/jquery.ui.datepicker.css",
                    "~/Content/themes/base/jquery.ui.progressbar.css",
                    "~/Content/themes/base/jquery.ui.theme.css"))

        bundles.Add(New StyleBundle("~/Vendor/css").Include(
                    "~/vendor/bootstrap/css/bootstrap.min.css",
                    "~/vendor/font-awesome/css/font-awesome.min.css",
                    "~/vendor/simple-line-icons/css/simple-line-icons.min.css",
                    "~/vendor/owl.carousel/assets/owl.carousel.min.css",
                    "~/vendor/owl.carousel/assets/owl.theme.default.min.css",
                    "~/vendor/magnific-popup/magnific-popup.min.css",
                    "~/vendor/rs-plugin/css/settings.css",
                    "~/vendor/rs-plugin/css/layers.css",
                    "~/vendor/rs-plugin/css/navigation.css",
                    "~/vendor/circle-flip-slideshow/css/component.css",
                    "~/vendor/pnotify/pnotify.custom.css"
                   ))


        bundles.Add(New StyleBundle("~/Content/theme").Include(
                    "~/content/theme.css",
                    "~/content/theme-elements.css",
                    "~/content/theme-blog.css",
                    "~/content/theme-shop.css",
                    "~/content/theme-animate.css",
                    "~/content/custom.css"
                   ))

        bundles.Add(New StyleBundle("~/Content/skin").Include(
                    "~/content/skins/skin-corporate-3.css"))


        BundleTable.EnableOptimizations = True



    End Sub
End Class