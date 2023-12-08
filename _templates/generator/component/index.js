const fs = require('fs');
const path = require('path');

module.exports = {
    params: ({ args }) => {
        const data = { ...args };

        const components = [];
        const root = '../../ionic/ionic-framework/core/src/components';
        const dirNames = fs.readdirSync(root);
        for (let dir of dirNames) {
            const component = {
                name: dir,
                includeBase: false,
                includeMd: false,
                includeIos: false,
            }
            const componentPath = path.resolve(root, dir);
            const fileNames = fs.readdirSync(componentPath);
            component.includeBase = fileNames.includes(`${dir}.scss`);
            component.includeMd = fileNames.includes(`${dir}.md.scss`);
            component.includeIos = fileNames.includes(`${dir}.ios.scss`);
            components.push(component);
        }
        data.context = {
            components: components
        };
        return data;
    }
}
