// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 5/2017
// Modified: 7/2017

var express = require('express')
  , routes = require('./routes')
  , user = require('./routes/user')
  , http = require('http')
  , path = require('path');
var app = express();

// Importing self-made modules
var EquipmentServer = require("./EquipmentServer");
var UaServer = require("./UaServer");


// How to disable view engine:
// http://stackoverflow.com/questions/17911228/how-do-i-use-html-as-the-view-engine-in-express

// all environments
app.set('port', process.env.PORT || 8088);
//app.set('views', __dirname + '/views');
//app.set('view engine', 'jshtml');
app.use(express.favicon());
app.use(express.logger('dev'));
app.use(express.bodyParser());
app.use(express.methodOverride());
//app.use(app.router);
app.use(express.static(path.join(__dirname, 'public')));

// development only
if ('development' === app.get('env'))
{
    app.use(express.errorHandler());
}

app.get('/', routes.index);
//app.get('/users', user.list);

http.createServer(app).listen(app.get('port'), function()
{
    console.log('Express server listening on port ' + app.get('port'));
});


// Server Side Events in Express
// https://www.npmjs.com/package/express-sse

var SSE = require('express-sse');
var sse = new SSE(); 
app.get('/stream', sse.init);

// Code from SSE author examples:
//var content = "data: Hello!";
//sse.send(content);
//sse.send(content, eventName);
//sse.send(content, eventName, customID);
//sse.updateInit(["array", "containing", "new", "content"]);
//sse.serialize(["array", "to", "be", "sent", "as", "serialized", "events"]);


// Instantiating servers
var equipmentServer = new EquipmentServer();
var uaServer = new UaServer(equipmentServer);

// Sending updates to GUI
setInterval(function()
{
	sse.send(equipmentServer.serialize());
 
}, 500); // Every 500 ms

// Running simulation cycles
setInterval(function()
{
	equipmentServer.runSimulation();
 
}, 100); // Every 100 ms

// Setting up a resource for simulation reset.
// While correct from the Restful point of view, not using the HTTP DELETE method
// due to its presumably limited browser support.
app.get('/reset', function(req, res)
{
	// Resetting simulation
	console.log("Got reset request, resetting simulation");
	equipmentServer.reset();
	res.send("OK");
});
