# Foundation Projects #
The following projects were pulled over from the Habitat solution and adapted to suit the needs of this project.
* Serialization
* Testing
* SitecoreExtensions
* Indexting
* Assets
* Dictionary
* Alerts

The Habitat project can be found at the following URL:
https://github.com/Sitecore/Habitat

### Steps for migrating foundation projects
1. Copy foundation module folder on file system
2. Create foundation module in VS Solution
3. Add existing foundation module project into solution
4. Rename project
5. Update project properties
   * Assembly Name
   * Default Namespace
   * Assembly Information
6. Update Version of Sitecore Nuget Packages to match working version, Sitecore 8.2 Update 2 (8.2.161221).
7. Adjust namespace references to reflect to the namespace specified in step 5.
8. If test project(s) exits for a foundation module, repeat steps 3-7 for each test project.
9. Update the list above with the foundation module name to recognize Habitat for the base code.