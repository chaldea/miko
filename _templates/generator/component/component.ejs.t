---
to: components.json
force: true
---
[<%for(let [i, component] of context.components.entries()){%>
    {
        "tag": "ion-<%= component.name %>",
        "srcDir": "src/components/<%= component.name %>",
        "include": ".scss",
        "specifics": [<% if(component.includeBase){ %>
            {
                "name": "<%= component.name %>.scss",
                "transform": true,
                "import": true
            }<% } %><% if(component.includeMd){ %>,
            {
                "name": "<%= component.name %>.md.scss",
                "transform": true,
                "import": true,
                "suffix": ".md"
            }<% } %><% if(component.includeIos){ %>,
            {
                "name": "<%= component.name %>.ios.scss",
                "transform": true,
                "import": true,
                "suffix": ".ios"
            }<% } %>
        ]
    },<%}%>
    {
        "tag": "ion-icon",
        "root": "../../ionic/ionicons",
        "srcDir": "src/components/icon",
        "include": ".css",
        "specifics": [
            {
                "name": "icon.css",
                "transform": true,
                "import": true,
                "extname": ".scss"
            }
        ]
    }
]