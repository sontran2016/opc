/* Copyright 2015, Google Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 *     * Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above
 * copyright notice, this list of conditions and the following disclaimer
 * in the documentation and/or other materials provided with the
 * distribution.
 *     * Neither the name of Google Inc. nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

var PROTO_PATH = './config/Rpc.proto';

var grpc = require('grpc');
var rpc_proto = grpc.load(PROTO_PATH).RpcPackage;

//    Implements the OPC RPC method. 
function sayHello(call, callback) {
    console.log("Receive hello request");
  callback(null, {message: 'Hello ' + call.request.name});
}

function sayHelloAgain(call, callback) {
    console.log("Receive hello again request");
    callback(null, { message: 'Hello again ' + call.request.name });
}

function mySum(call, callback) {
    console.log("Receive sum request");
    callback(null, { result: call.request.a+call.request.b });
}

function testConnection(call, callback) {
    console.log("Receive test connection request");
    callback(null,{status: true});
}

function receiveGroupTags(call, callback) {
    try {
        //console.log(JSON.stringify(call.request));
        call.request.Tags.forEach(function(item) {
            console.log(item.Name + ": " + item.Value);
        });
        callback(null, { message: 'ok' });
    } catch (e) {
        console.log(e);
        callback(null, { message: e.message });
    } 
}


/*
 * Starts an RPC server that receives requests for the RpcService service at the
 * sample server port
 */
function main() {
      var ip = "192.168.1.79:50051";  //0.0.0.0:50051
      var server = new grpc.Server();
      server.addProtoService(rpc_proto.RpcService.service, {
          sayHello: sayHello, sayHelloAgain: sayHelloAgain, mySum: mySum,
          receiveGroupTags: receiveGroupTags, testConnection: testConnection
      });
      server.bind(ip, grpc.ServerCredentials.createInsecure());
      server.start();  
}

main();
