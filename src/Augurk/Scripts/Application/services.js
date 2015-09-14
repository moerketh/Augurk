﻿/*
 Copyright 2014-2015, Mark Taling
 
 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at
 
 http://www.apache.org/licenses/LICENSE-2.0
 
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
*/

var AugurkServices = angular.module('AugurkServices', ['ngResource', 'ngRoute']);

AugurkServices.factory('featureService', ['$resource', function ($resource) {
    // The featurename might contain a period, which webapi only allows if you finish with a slash
    // Since AngularJS doesn't allow for trailing slashes, use a backslash instead
    return $resource('api/v2/products/:productName/groups/:groupName/features/:featureName\\',
                     { productName: '@productName', groupName: '@groupName', featureName: '@featureName' });
}]);

AugurkServices.factory('featureDescriptionService', ['$resource', function ($resource) {

    // The branchname might contain a period, which webapi only allows if you finish with a slash
    // Since AngularJS doesn't allow for trailing slashes, use a backslash instead
    var service = {
        getFeaturesByBranch: function (branch, callback) {
            $resource('api/features/:branchName\\', { branchName: '@branchName' })
                .query({ branchName: branch }, callback);
        },

        getFeaturesByBranchAndTag: function (branch, tag, callback) {
            $resource('api/tags/:branchName/:tag/features', { branchName: '@branchName', tag: '@tag' })
                .query({ branchName: branch, tag: tag }, callback);
        },

        getGroupsByProduct: function (product, callback) {
            $resource('api/v2/products/:productName/groups', { productName: '@productName' })
                .query({ productName: product }, callback);
        }
    };
    
    return service;
}]);

AugurkServices.factory('productService', ['$http', '$q', '$routeParams', '$rootScope', function ($http, $q, $routeParams, $rootScope) {

    // create the service
    var service = {
        products: null,
        currentProduct: null
    };

    // since AngularJS' $resource does not support primitive types, use $http instead.
    var productsPromiseDeferrer = $q.defer();
    $http({ method: 'GET', url: 'api/v2/products' }).then(function (response) {
        productsPromiseDeferrer.resolve(response.data);
    });

    service.products = productsPromiseDeferrer.promise;

    // set the current product
    if ($routeParams.productName) {
        service.currentProduct = $routeParams.productName;
    }
    else {
        productsPromiseDeferrer.promise.then(function (products) {
            service.currentProduct = products[0];
            $rootScope.$broadcast('currentProductChanged', { product: service.currentProduct });
        });
    }

    // update the product on navigation
    $rootScope.$on('$routeChangeSuccess', function () {
        if ($routeParams.productName &&
            $routeParams.productName != service.currentProduct) {
            service.currentProduct = $routeParams.productName;
            $rootScope.$broadcast('currentProductChanged', { product: service.currentProduct });
        }
    });

    return service;
}]);