const fs = require('fs');
const path = require('path')
const { exec } = require("child_process");

const src = [
    'src/components/app/app.scss',
    'src/components/button/button.ios.scss',
    'src/components/button/button.md.scss',
    'src/components/content/content.scss',
    'src/components/header/header.ios.scss',
    'src/components/header/header.md.scss',
    'src/components/title/title.ios.scss',
    'src/components/title/title.md.scss',
    'src/components/toolbar/toolbar.ios.scss',
    'src/components/toolbar/toolbar.md.scss',
]

const components = {
    'ion-button': ['src/components/button/button.scss'],
    'ion-button.md': ['src/components/button/button.md.scss'],
    'ion-button.ios': ['src/components/button/button.ios.scss'],
    'ion-content': ['src/components/content/content.scss'],
    'ion-title': ['src/components/title/title.scss'],
    'ion-title.md': ['src/components/title/title.md.scss'],
    'ion-title.ios': ['src/components/title/title.ios.scss'],
    'ion-toolbar': ['src/components/toolbar/toolbar.scss'],
    'ion-toolbar.md': ['src/components/toolbar/toolbar.md.scss'],
    'ion-toolbar.ios': ['src/components/toolbar/toolbar.ios.scss']
}

function createBundle(dist, bundle) {
    return new Promise((resolve, reject) => {
        exec(`"node_modules/.bin/cleancss" -o ${bundle} ${dist}`, (error, stdout) => {
            if (error) {
                reject(error);
            } else {
                resolve(stdout);
            }
        });
    });
}

function createCss(source, dist) {
    return new Promise((resolve, reject) => {
        exec(`"node_modules/.bin/sass" "${source}:${dist}"`, (error, stdout) => {
            if (error) {
                reject(error)
                return;
            }
            resolve(stdout);
        });
    });
}

function getCss(source) {
    const bundle = './dist/bundle.css';
    const dist = './dist/style.css';
    return new Promise((resolve, reject) => {
        createCss(source, dist).then(res => createBundle(dist, bundle)).then(x => {
            try {
                const data = fs.readFileSync(bundle, 'utf8');
                fs.unlinkSync(bundle);
                fs.unlinkSync(dist);
                fs.unlinkSync(`${dist}.map`);
                resolve(data);
            } catch (error) {
                reject(error);
            }
        }).catch(error => {
            reject(error);
        })
    })
}

async function generate() {
    let template = '';
    template += 'export const styles = {\n'
    for (const item of src) {
        const fileName = path.basename(item);
        if (fs.existsSync(item)) {
            console.log(item);
            const css = await getCss(item);
            template += `    "${fileName}": \`${css}\`,\n`;
        }
    }
    template += `};`;
    try {
        fs.writeFileSync('./src/components/core/JsInterop/style.ts', template);
    } catch (err) {
        console.error(err)
    }
}

function convert() {
    const srcPath = '../../ionic/ionic-framework/core';
    const regex1 = /:host\(([\s\S]*?)\)([\s|,])|:host/gm;
    const regex2 = /::slotted\(([\s\S]*?)\)/gm;
    for (const name in components) {
        const src = components[name];
        for (const item of src) {
            const file = fs.readFileSync(path.resolve(srcPath, item), 'utf8');
            const content = file.replace(regex1, `${name}$1$2`).replace(regex2, `${name} $1`);
            fs.writeFileSync(item, content);
        }
    }
}
convert();