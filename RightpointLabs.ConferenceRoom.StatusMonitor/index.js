var fs = require('fs');
var http = require('http');
var url = require('url');
var Promise = require('promise');
var gpio = require('rpi-gpio');
var async = require('async');
var signalR = require('signalr-client');

var config = JSON.parse(fs.readFileSync('config.json'));

async.parallel([
    function(c) {
        gpio.setup(config.redPin, gpio.DIR_OUT, c);
    },
    function(c) {
        gpio.setup(config.greenPin, gpio.DIR_OUT, c);
    }
], function (e, results) {
    console.log('gpio setup');
    console.log(e);
    console.log(results);
});

function getStatus() {
    return new Promise(function(resolve, reject) {
        var options = url.parse(config.apiServer + "/room/" + config.room + "/status");
        options.method = "GET";
        return http.request(options, function(e) {
            data = "";
            e.on('data', function(c) { data += String(c); });
            e.on('end', function() { resolve(data); });
        }).end();
    })
}

var updateTimeout = null;
function updateIn(delay) {
    if(null != updateTimeout){
        clearTimeout(updateTimeout);
    }
    updateTimeout = setTimeout(function() {
        getStatus().then(function(data) {
            var status = JSON.parse(data).Status;
            switch(status) {
                case 0:
                    green();
                    break;
                case 1:
                    red();
                    break;
                case 2:
                    red();
                    break;
                default:
                    console.log('invalid status: ' + status);
                    break;
            }
        });
    }, delay);
}

function red() {
    console.log('red');
    gio.write(config.redPin, function(e) {
        if(e) {
            console.log(e);
        }
    });
}
function green() {
    console.log('green');
    gio.write(config.greenPin, function(e) {
        if(e) {
            console.log(e);
        }
    });
}

setInterval(function() {
    updateIn(5000);
}, 5 * 60 * 1000);

var client  = new signalR.client(
    config.signalRServer,
    ['UpdateHub']
);
client.on('UpdateHub', 'Update', function(room) {
    console.log('got notification of change to ' + room);
    if(config.room == room) {
        updateIn(1);
    }
});

updateIn(1);

// wait
