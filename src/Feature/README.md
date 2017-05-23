# Feature Projects #
The following projects were pulled over from the Habitat solution and adapted to suit the needs of this project.
* Metadata
* Identity
* {Module Name Here}

The Habitat project can be found at the following URL:
https://github.com/Sitecore/Habitat

### Steps for migrating feature projects
1. Copy feature module folder on file system
2. Create feature module in VS Solution
3. Add existing feature module project into solution
4. Rename project
5. Update project properties
   * Assembly Name
   * Default Namespace
   * Assembly Information
6. Update Version of Sitecore Nuget Packages to match working version, Sitecore 8.2 Update 2 (8.2.161221).
7. Adjust namespace references to reflect to the namespace specified in step 5.
8. If test project(s) exits for a feature module, repeat steps 3-7 for each test project.
9. Update the list above with the feature module name to recognize Habitat for the base code.