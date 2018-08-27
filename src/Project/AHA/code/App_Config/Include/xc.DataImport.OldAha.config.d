<?xml version="1.0" encoding="utf-8"?>
<configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
  <sitecore>
    <databases>
      <!-- oldaha -->
      <database id="oldaha" singleInstance="true" type="Sitecore.Data.DefaultDatabase, Sitecore.Kernel" >
		  <param desc="name">$(id)</param>
		  <icon>Images/database_master.png</icon>
		  <dataProviders hint="list:AddDataProvider">
			<dataProvider ref="dataProviders/main" param1="$(id)">
			  <prefetch hint="raw:AddPrefetch">
				<sc.include file="/App_Config/Prefetch/Common.config" />
				
			  </prefetch>
			</dataProvider>
		  </dataProviders>
		  <securityEnabled>true</securityEnabled>
		  <publishVirtualItems>true</publishVirtualItems>
		  <PropertyStore ref="PropertyStoreProvider/store[@name='$(id)']" />
		  <remoteEvents.EventQueue>
			<obj ref="eventing/eventQueueProvider/eventQueue[@name='$(id)']" />
		  </remoteEvents.EventQueue>
		  <workflowProvider hint="defer" type="Sitecore.Workflows.Simple.WorkflowProvider, Sitecore.Kernel">
			<param desc="database">$(id)</param>
			<param desc="history store" ref="workflowHistoryStores/main" param1="$(id)" />
		  </workflowProvider>
		  <NotificationProvider type="Sitecore.Data.DataProviders.$(database).$(database)NotificationProvider, Sitecore.Kernel">
			<param connectionStringName="$(id)">
			</param>
			<param desc="databaseName">$(id)</param>
		  </NotificationProvider>
		  <cacheSizes hint="setting">
			<data>0MB</data>
			<items>0MB</items>
			<paths>0KB</paths>
			<itempaths>0MB</itempaths>
			<standardValues>0KB</standardValues>
		  </cacheSizes>
		</database>
    </databases>
	<PropertyStoreProvider>
		<store name="oldaha" prefix="oldaha" getValueWithoutPrefix="true" singleInstance="true" type="Sitecore.Data.Properties.$(database)PropertyStore, Sitecore.Kernel">
		  <param ref="dataApis/dataApi[@name='$(database)']" param1="$(name)" />
		  <param resolve="true" type="Sitecore.Abstractions.BaseEventManager, Sitecore.Kernel" />
		  <param resolve="true" type="Sitecore.Abstractions.BaseCacheManager, Sitecore.Kernel" />
		</store>
	</PropertyStoreProvider>
	<eventing>
		<eventQueueProvider>
		  <eventQueue name="oldaha" type="Sitecore.Data.Eventing.$(database)EventQueue, Sitecore.Kernel">
			<param ref="dataApis/dataApi[@name='$(database)']" param1="$(name)" />
			<param hint="" ref="PropertyStoreProvider/store[@name='$(name)']" />
		  </eventQueue>
		</eventQueueProvider>
	</eventing>
  </sitecore>
</configuration>