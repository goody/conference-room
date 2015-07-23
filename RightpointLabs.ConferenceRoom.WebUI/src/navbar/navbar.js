(function() {
    'use strict;'

    angular.module('app').directive('navbar', ['settings', function(settings) { /* settings reference makes sure it gets loaded */
        return {
            restrict: 'A',
            templateUrl: 'navbar/navbar.html',
            replace: true
        };
    }])
})();