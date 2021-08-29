const fs = require('fs');
const path = require('path')
const { exec } = require("child_process");

const components = {
    'ion-button': 'src/components/button',
    'ion-toolbar': 'src/components/toolbar',
    'ion-content': 'src/components/content'
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
    template += 'const styleMap = {\n'
    for (const key in components) {
        const root = components[key];
        const fileName = path.basename(components[key]);
        const base = `${root}/${fileName}.scss`;
        const ios = `${root}/${fileName}.ios.scss`;
        const md = `${root}/${fileName}.md.scss`;
        if(fs.existsSync(ios) && fs.existsSync(md)) {
            console.log(ios);
            console.log(md);
            const iosCss = await getCss(ios);
            const mdCss = await getCss(md);
            template += `    "${key}-md": \`${mdCss}\`,\n`;
            template += `    "${key}-ios": \`${iosCss}\`,\n`;
        } else if(fs.existsSync(base)){
            console.log(base);
            const css = await getCss(base);
            template += `    "${key}": \`${css}\`,\n`;
        }
    }
    template += `};

const styleCache: { [key: string]: any } = {};

export function getStyle(name: string, type: string): CSSStyleSheet {
    let key = name;
    if (type) {
        key = \`\${name}-\${type}\`;
    }
    if (!styleCache[key]) {
        styleCache[key] = new CSSStyleSheet();
        styleCache[key].replace(styleMap[key]);
    }
    return styleCache[key];
}`;
    try {
        fs.writeFileSync('./src/components/core/JsInterop/style.ts', template);
    } catch (err) {
        console.error(err)
    }
}

generate();