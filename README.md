<h1 align="center">
  <br>
  <a href="https://redditinsight.pro"><img src="https://github.com/zeminlai/RedditInsightAngular/assets/106502102/b4189a27-16bc-499b-896d-d2bcce315639" alt="Markdownify" width="1000"></a>

</h1>
<h4 align="center">A superior way to search Reddit</h4>

<p align="center">
<img align="center" alt="TypeScript" width="30px" style="padding-right:10px;" src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/typescript/typescript-plain.svg" />
<img align="center" alt="Angular" width="30px" style="padding-right:10px;" src="https://cdn.jsdelivr.net/gh/devicons/devicon/icons/angularjs/angularjs-plain.svg" />
<img align="center" alt="C#" width="30px" style="padding-right:10px;" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/csharp/csharp-plain.svg" />
<img align="center" alt=".Net Core" width="30px" style="padding-right:10px;" src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/dotnetcore/dotnetcore-original.svg" />
</p>

##  Usage
1. Try it out at [redditinsight.pro](https://redditinsight.pro/)!
2. Enter in your search term 
3. Read up on best matched Reddit posts on that topic  

##  Backend API Notes
[Frontend Notes](https://github.com/zeminlai/RedditInsightAngular)

 - The API was built using .Net Core 6 with [HTMLAgilityPack](https://html-agility-pack.net/) and [Reddit .Net](https://github.com/sirkris/Reddit.NET)
 - Request to Google's "hidden" api along with the user's search term, which returns a full HTML google search results page
 - HTMLAgilityPack was used to parse the HTML page for Reddit url links
 - Reddit .Net was then used to retrieve the Reddit posts 
