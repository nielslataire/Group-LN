Imports System.Web.Optimization

Public Module BundleConfig
    ' For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
    Public Sub RegisterBundles(ByVal bundles As BundleCollection)

        bundles.Add(New ScriptBundle("~/bundles/jquery").Include(
                    "~/vendor/jquery/jquery.js"))

        bundles.Add(New ScriptBundle("~/bundles/jqueryval").Include(
                    "~/Scripts/jquery.validate*"))

        ' Use the development version of Modernizr to develop with and learn from. Then, when you're
        ' ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
        bundles.Add(New ScriptBundle("~/bundles/modernizr").Include(
                    "~/Scripts/modernizr-*"))

        bundles.Add(New ScriptBundle("~/bundles/admin/modernizr").Include(
            "~/vendor/admin/modernizr/modernizr.js"))

        bundles.Add(New ScriptBundle("~/bundles/bootstrap").Include(
                  "~/vendor/bootstrap/bootstrap.js",
                  "~/Scripts/respond.js"))

        bundles.Add(New StyleBundle("~/Content/css").Include(
                  "~/vendor/bootstrap/bootstrap.css"))

        bundles.Add(New StyleBundle("~/Vendor/css").Include(
                   "~/vendor/fontawesome/css/font-awesome.css",
                   "~/vendor/owlcarousel/owl.carousel.min.css",
                   "~/vendor/owlcarousel/owl.carousel.min.css",
                   "~/vendor/magnific-popup/magnific-popup.css",
                   "~/vendor/owlcarousel/owl.theme.default.min.css",
                   "~/vendor/rs-plugin/css/settings.css",
                   "~/vendor/circle-flip-slideshow/css/component.css",
                   "~/vendor/mediaelement/mediaelementplayer.css"))

        bundles.Add(New StyleBundle("~/Vendor/Admin/css").Include(
           "~/vendor/admin/bootstrap/css/bootstrap.css",
           "~/vendor/admin/font-awesome/css/font-awesome.css",
           "~/vendor/admin/elusive-icons/css/elusive-webfont.css",
           "~/vendor/admin/magnific-popup/magnific-popup.css",
           "~/vendor/admin/bootstrap-datepicker/css/datepicker3.css",
            "~/vendor/admin/pnotify/pnotify.custom.css",
            "~/vendor/admin/select2/select2.css",
            "~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.css",
            "~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.css",
            "~/vendor/admin/select2/select2-bootstrap.css",
            "~/vendor/admin/isotope/jquery.isotope.css"
           ))




        bundles.Add(New StyleBundle("~/Theme/css").Include(
                  "~/Content/theme.css",
                  "~/Content/theme-elements.css",
                  "~/Content/theme-blog.css",
                  "~/Content/theme-shop.css",
                  "~/Content/theme-animate.css",
                  "~/Content/skins/copro.css",
                  "~/Content/custom.css"))
        bundles.Add(New StyleBundle("~/Theme/Admin/css").Include(
          "~/Content/Admin/theme.css",
           "~/Content/Admin/skins/skin.css",
            "~/Content/Admin/skins/square-borders.css",
          "~/Content/Admin/theme-custom.css"
         ))



        bundles.Add(New ScriptBundle("~/bundles/vendor").Include(
                "~/vendor/jquery.appear/jquery.appear.js",
                "~/vendor/jquery.easing/jquery.easing.js",
                "~/vendor/jquery-cookie/jquery-cookie.jss",
                "~/vendor/common/common.js",
                "~/vendor/jquery.validation/jquery.validation.js",
                "~/vendor/jquery.stellar/jquery.stellar.js",
                "~/vendor/jquery.easy-pie-chart/jquery.easy-pie-chart.js",
                "~/vendor/jquery.gmap/jquery.gmap.js",
                "~/vendor/isotope/jquery.isotope.js",
                "~/vendor/owlcarousel/owl.carousel.js",
                "~/vendor/jflickrfeed/jflickrfeed.js",
                "~/vendor/magnific-popup/jquery.magnific-popup.js",
                "~/vendor/vide/vide.js",
                "~/vendor/rs-plugin/js/jquery.themepunch.tools.min.js",
                "~/vendor/rs-plugin/js/jquery.themepunch.revolution.min.js",
                "~/scripts/examples.notifications.js",
                "~/vendor/circle-flip-slideshow/js/jquery.flipshow.js"))

        bundles.Add(New ScriptBundle("~/bundles/admin/vendor").Include(
               "~/vendor/admin/jquery/jquery.js",
               "~/vendor/admin/jquery-browser-mobile/jquery.browser.mobile.js",
               "~/vendor/admin/bootstrap/js/bootstrap.js",
               "~/vendor/admin/nanoscroller/nanoscroller.js",
               "~/vendor/admin/bootstrap-datepicker/js/bootstrap-datepicker.js",
               "~/vendor/admin/bootstrap-datepicker/js/locales/bootstrap-datepicker.nl-BE.js",
               "~/vendor/admin/magnific-popup/magnific-popup.js",
               "~/scripts/admin/ui-elements/examples.modals.js",
               "~/vendor/admin/jquery-maskedinput/jquery.maskedinput.js",
               "~/vendor/admin/pnotify/pnotify.custom.js",
               "~/vendor/admin/jquery-placeholder/jquery.placeholder.js",
                "~/vendor/admin/select2/select2.js",
                "~/vendor/admin/select2/select2_locale_nl.js",
                "~/vendor/admin/bootstrap-multiselect/bootstrap-multiselect.js",
                "~/vendor/admin/isotope/jquery.isotope.js",
                "~/vendor/admin/bootstrap-fileupload/bootstrap-fileupload.min.js",
                "~/vendor/admin/bootstrap-tagsinput/bootstrap-tagsinput.js",
                "~/vendor/admin/bootstrap-maxlength/bootstrap-maxlength.js",
                "~/vendor/admin/jquery-autosize/jquery.autosize.js",
                 "~/Scripts/admin/pages/examples.mediagallery.js",
                 "~/vendor/admin/jquery-validation/jquery.fixes.js"
))

        bundles.Add(New ScriptBundle("~/bundles/theme").Include(
                        "~/Scripts/theme.js",
                        "~/Scripts/views/view.home.js",
                        "~/Scripts/custom.js",
                "~/Scripts/theme.init.js"))

        bundles.Add(New ScriptBundle("~/bundles/admin/theme").Include(
                        "~/Scripts/admin/theme.js",
                        "~/Scripts/admin/custom.js",
                        "~/Scripts/admin/theme.custom.js",
                        "~/Scripts/admin/theme.init.js",
                         "~/Scripts/theme.js",
                         "~/Scripts/theme.init.js"))
    End Sub
End Module

