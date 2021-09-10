# http
项目中demo下都是不同http示例的实现，直接在program里面实例化服务，浏览器预览效果即可。
## 示例
|示例|说明|
|--|--|
|00、demo/HelloWorld.cs|发送hello world!到客户端|
|01、demo/HelloWorldWithResponser.cs|使用HttpResponser发送hello world!到客户端|
|02、demo/HelloWorldWithChunked.cs|使用ChunkedResponser发送hello world!到客户端|
|03、demo/BufferedStreamTest.cs|BufferedNetworkStream测试|
|04、demo/Gzip.cs|Gzip压缩|
|05、demo/SendJsFile.cs|Gzip压缩发送一个JS文件到客户端|
|06、demo/PostAndQueryString.cs|解析POST请求和查询字符串|
|07、demo/KeepAliveTest.cs|在一个连接中处理多个HTTP请求|
|08、demo/HttpServerUpload.cs|处理HTTP上传|
|09、demo/WebSocketTest.cs|WebSocket测试|