# NET NEWS HTML

This Project is inspired form PHP-NEWS-HTML and 68k.news. This .NET Project is a simple proxy project for well known news portal in Indonesia, presented in non secured http protocol for older devices. 

This project use some dependency, such as :
1. AngleSharp
2. NRedis and Redis Server
3. EF Core SQLite

This project will replace http://news.benyamin.xyz now that use PHP, and moved it into .NET, as newer 8.0 LTS already released and quite powerful for a lot of load. 

The PHP NEWS HTML will still be maintained for hobby purpose with other improvement.  

## Saving to Server 
This feature use .sqlite file, for now the layer is using interface, so if in the future we need to change the layer, we can change the implementation. 

All data structure is in `Models`, and the implementation is in `Library`