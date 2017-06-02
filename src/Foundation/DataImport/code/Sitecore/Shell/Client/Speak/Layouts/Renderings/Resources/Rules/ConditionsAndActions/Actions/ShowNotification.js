define([], function () {
    var action = function (context, args) {

        var targetControl = context.app[args.controlId], notifications = context.args[0] ? context.args[0].get("messages") : null, notificationForMessageBar;

        if (targetControl == null) {
            throw "targetControl not found";
        }

        _.each(notifications, function (notification) {
            notificationForMessageBar = {
                text: notification.Text,
                actions: [],
                closable: false
            };
            targetControl.addMessage("notification", notificationForMessageBar);
        });
    };

    return action;
});
