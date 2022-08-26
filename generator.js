const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const components = {
    'ion-button': ['src/components/button/button.scss'],
    'ion-button.md': ['src/components/button/button.md.scss'],
    'ion-button.ios': ['src/components/button/button.ios.scss'],
    'ion-content': ['src/components/content/content.scss'],
    'ion-item': ['src/components/item/item.scss'],
    'ion-item.md': ['src/components/item/item.md.scss'],
    'ion-item.ios': ['src/components/item/item.ios.scss'],
    'ion-label': ['src/components/label/label.scss'],
    'ion-label.md': ['src/components/label/label.md.scss'],
    'ion-label.ios': ['src/components/label/label.ios.scss'],
    'ion-tab': ['src/components/tab/tab.scss'],
    'ion-tabs': ['src/components/tabs/tabs.scss'],
    'ion-tab-bar': ['src/components/tab-bar/tab-bar.scss'],
    'ion-tab-bar.md': ['src/components/tab-bar/tab-bar.md.scss'],
    'ion-tab-bar.ios': ['src/components/tab-bar/tab-bar.ios.scss'],
    'ion-tab-button': ['src/components/tab-button/tab-button.scss'],
    'ion-tab-button.md': ['src/components/tab-button/tab-button.md.scss'],
    'ion-tab-button.ios': ['src/components/tab-button/tab-button.ios.scss'],
    'ion-title': ['src/components/title/title.scss'],
    'ion-title.md': ['src/components/title/title.md.scss'],
    'ion-title.ios': ['src/components/title/title.ios.scss'],
    'ion-toolbar': ['src/components/toolbar/toolbar.scss'],
    'ion-toolbar.md': ['src/components/toolbar/toolbar.md.scss'],
    'ion-toolbar.ios': ['src/components/toolbar/toolbar.ios.scss'],
}

const slotted1 = /(?: )::slotted\(([\s\S]*?)\)(?!\))/gm;    // match ::slotted
const slotted2 = /(?:\):):slotted\(([\s\S]*?)\)(?!\))/gm;   // match ::slotted
const slotted3 = /::slotted\(([\s\S]*?)\)(?!\))/gm;         // match ::slotted
const host1 = /:host-context\(([\s\S]*?)\)(?!\))/gm;        // match :host-context
const host2 = /:host(?!-)(?:\(([\s\S]*?)\))*([\s|,])/gm;    // match :host

function transform(name, content) {
    return content
        .replace(slotted1, ` slot $1`)
        .replace(slotted2, `) slot $1`)
        .replace(slotted3, `${name} slot $1`)
        .replace(host1, `$1 ${name}`)
        .replace(host2, `${name}$1$2`)
}

function generateCss() {
    const srcPath = '../../ionic/ionic-framework/core';
    for (const name in components) {
        const src = components[name];
        for (const item of src) {
            const styleFile = path.resolve(srcPath, item);
            console.log(styleFile);
            const file = fs.readFileSync(styleFile, 'utf8');
            const content = transform(name, file);
            fs.writeFileSync(item, content);
        }
    }
}

function generateIcons() {
    const stylePath = '../../ionic/ionicons/src/components/icon/icon.css'
    const style = fs.readFileSync(stylePath, 'utf8');
    const content = transform('ion-icon', style);
    fs.writeFileSync('src/components/icon/icon.scss', content);
    const context = {
        path: 'src/components/icon/IconResource.cs',
        svgRoot: '../../ionic/ionicons/src/svg'
    }
    execSync(`"./node_modules/.bin/hygen" generator icon icon --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

generateCss();

generateIcons();