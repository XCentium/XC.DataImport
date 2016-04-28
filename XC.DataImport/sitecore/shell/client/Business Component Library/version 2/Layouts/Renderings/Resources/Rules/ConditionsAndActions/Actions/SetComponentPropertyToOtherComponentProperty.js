define([], function() {
  var action = function(context, args) {
      var targetControl = context.app[args.targetControlId],
          propertyName = args.propertyName,
          sourceControl = context.app[args.sourceControlId],
          sourceProperty = args.sourceProperty,
          selectedItem;

      if (targetControl == null) {
          throw "targetControl not found";
      }
      if (!propertyName) {
          throw "propertyName is not set";
      }

      if (sourceControl.get(sourceProperty)) {
          selectedItem = sourceControl.get(sourceProperty);
      } else {
          console.debug("Unable to get the property to set");
          return;
      }

      console.log(propertyName);
      var pluginOptions = targetControl.pluginOptions;
      if (pluginOptions) {
          pluginOptions[propertyName] = selectedItem;
      }
      targetControl[propertyName] = selectedItem;
  };

  return action;
});