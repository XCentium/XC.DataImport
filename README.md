# XC.DataImport


Run SQL Data Import Mappings in the following order.  NOTE: can use the following temp directory to monitor status if the interface stops refreshing.

\Website\temp\importstatus

1. Keywords   
2. Media Content Folders
3. Excel Documents - CE
4. Excel Documents - SP
5. PDF Documents - CE.json
6. PDF Documents - SP.json
7. Powerpoint Documents - CE
8. Powerpoint Documents - SP
9. Txt Files
10. Word Documents - CE
11. Word Documents - SP
12. Flash Preview
13. Banner Images
14. Feature Images
15. GIF Images
16. Ico Images
17. Infographic Images
18. JPEG Images
19. PNG Images
20. Thumbnail Images
21. Sections
22. Content Folders
23. Article - Part 1 (1 - 300)
24. Article - Part 2 (301 - 600)
25. Article - Part 3 (601 - 900)
26. Article - Part 4 (901 - 1200)
27. Article - Part 5 (1201 - 1500)
28. Article - Part 6 (1501 - 1800)
29. Article - Part 7 (1801 - 2100)
30. Article - Part 8 (2101 - 2400)
31. Article - Part 9 (2401 - 2700)
32. Article - Part 10 (2701 - 3000)
32. Events
33. Infographics
34. Magazines
35. Content References

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
