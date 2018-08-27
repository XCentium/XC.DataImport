<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
  <sitecore>
    <unicorn>
      <configurations>
        <configuration name="XC.Project.DataImport" description="Project Specific DataImport" dependencies="Foundation.Serialization,Foundation.DataImport" patch:after="configuration[@name='Foundation.Serialization']">
          <targetDataStore name="Foundation.DataImport.SpeakApplication" physicalRootPath="$(xc.dataimport.sourceFolder)\project\dataimport\serialization" type="Rainbow.Storage.SerializationFileSystemDataStore, Rainbow" useDataCache="false" singleInstance="true" />
          <predicate type="Unicorn.Predicates.SerializationPresetPredicate, Unicorn" singleInstance="true">
            <!-- Templates -->
            <include name="XC.Project.DataImport.Templates" database="master" path="/sitecore/templates/Project/Data Import" /><!---->
            
          </predicate>
        </configuration>
      </configurations>
    </unicorn>
  </sitecore>
</configuration>