# XC.DataImport


Run SQL Data Import Mappings in the following order.  NOTE: can use the following temp directory to monitor status if the interface stops refreshing.

\Website\temp\importstatus

1. Keywords   
2. Media Content Folders - AllElse
3. Media Content Folders - CE
4. Media Content Folders - SP
5. Excel Documents - AllElse
6. Excel Documents - CE
7. Excel Documents - SP
8. PDF Documents - AllElse.json
9. PDF Documents - CE.json
10. PDF Documents - SP.json
11. Powerpoint Documents - AllElse
12. Powerpoint Documents - CE    
13. Powerpoint Documents - SP
14. Txt Files - AllElse
15. Txt Files - CE
16. Txt Files - SP
17. Word Documents - AllElse
18. Word Documents - CE
19. Word Documents - SP
20. Flash Preview - AllElse
21. Flash Preview - CE
22. Flash Preview - SP
23. Banner Images - AlLElse
24. Banner Images - CE
25. Banner Images - SP
26. Feature Images - AllElse
27. Feature Images - CE
28. Feature Images - SP
29. GIF Images - AllElse
30. GIF Images - CE
31. GIF Images - SP
32. Ico Images - AllElse
33. Ico Images - CE
34. Ico Images - SP
35. Infographic Images - AllElse
36. Infographic Images - CE
37. Infographic Images - SP
38. JPEG Images - AllElse
39. JPEG Images - CE
40. JPEG Images - SP
41. PNG Images - AllElse
42. PNG Images - CE
43. PNG Images - SP
44. Thumbnail Images - AllElse
45. Thumbnail Images - CE
46. Thumbnail Images - SP
32. Sections
33. Content Folders
34. Article - Part 1 (1 - 300)
35. Article - Part 2 (301 - 600)
36. Article - Part 3 (601 - 900)
37. Article - Part 4 (901 - 1200)
38. Article - Part 5 (1201 - 1500)
39. Article - Part 6 (1501 - 1800)
40. Article - Part 7 (1801 - 2100)
41. Article - Part 8 (2101 - 2400)
42. Article - Part 9 (2401 - 2700)
43. Article - Part 10 (2701 - 3000)
44. Events
45. Infographics
46. Magazines
47. Content References

After mappings have completed, execute the following post import controller actions

localhost/api/sitecore/Import/\{controller action}

1. ArrangeItemsUnderParents
   1.  Articles  
   2.  Content Folders
   3.  Events
   4.  Infographics
   5.  Magazines
   6.  Sections
   7.  Documents
   8.  Flash
   9.  Images
   10. Media Folders
2. MoveMediaIntoML
3. RemoveDuplicateMediaReferences
4. CleanUpMediaReferenceItems
5. UpdateReferences
6. Generate301RedirectsForIIS
