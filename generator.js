const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const components = [
    // {
    //     tag: 'ion-accordion',
    //     srcDir: 'src/components/accordion',
    //     include: '.scss',
    //     specifics: [
    //         {
    //             name: 'accordion.scss',
    //             transform: true,
    //         },
    //         {
    //             name: 'accordion.md.scss',
    //             transform: true,
    //             import: true,
    //             suffix: '.md'
    //         },
    //         {
    //             name: 'accordion.ios.scss',
    //             transform: true,
    //             import: true,
    //             suffix: '.ios'
    //         },
    //     ]
    // },
    {
        tag: 'ion-app',
        srcDir: 'src/components/app',
        include: '.scss',
        specifics: [
            {
                name: 'app.scss',
                import: true,
            }
        ]
    },
    {
        tag: 'ion-button',
        srcDir: 'src/components/button',
        include: '.scss',
        specifics: [
            {
                name: 'button.scss',
                transform: true,
            },
            {
                name: 'button.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'button.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-content',
        srcDir: 'src/components/content',
        include: '.scss',
        specifics: [
            {
                name: 'content.scss',
                transform: true,
                import: true,
            },
        ]
    },
    {
        tag: 'ion-header',
        srcDir: 'src/components/header',
        include: '.scss',
        specifics: [
            {
                name: 'header.md.scss',
                import: true,
            },
            {
                name: 'header.ios.scss',
                import: true,
            }
        ]
    },
    {
        tag: 'ion-icon',
        root: '../../ionic/ionicons',
        srcDir: 'src/components/icon',
        include: '.css',
        specifics: [
            {
                name: 'icon.css',
                transform: true,
                import: true,
                extname: '.scss'
            }
        ]
    },
    {
        tag: 'ion-item',
        srcDir: 'src/components/item',
        include: '.scss',
        specifics: [
            {
                name: 'item.scss',
                transform: true,
            },
            {
                name: 'item.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'item.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-label',
        srcDir: 'src/components/label',
        include: '.scss',
        specifics: [
            {
                name: 'label.scss',
                transform: true,
            },
            {
                name: 'label.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'label.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-menu',
        srcDir: 'src/components/menu',
        include: '.scss',
        specifics: [
            {
                name: 'menu.scss',
                transform: true,
            },
            {
                name: 'menu.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'menu.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-tab',
        srcDir: 'src/components/tab',
        include: '.scss',
        specifics: [
            {
                name: 'tab.scss',
                transform: true,
                import: true,
            }
        ]
    },
    {
        tag: 'ion-tabs',
        srcDir: 'src/components/tabs',
        include: '.scss',
        specifics: [
            {
                name: 'tabs.scss',
                transform: true,
                import: true,
            }
        ]
    },
    {
        tag: 'ion-tab-bar',
        srcDir: 'src/components/tab-bar',
        include: '.scss',
        specifics: [
            {
                name: 'tab-bar.scss',
                transform: true,
            },
            {
                name: 'tab-bar.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'tab-bar.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-tab-button',
        srcDir: 'src/components/tab-button',
        include: '.scss',
        specifics: [
            {
                name: 'tab-button.scss',
                transform: true,
            },
            {
                name: 'tab-button.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'tab-button.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-title',
        srcDir: 'src/components/title',
        include: '.scss',
        specifics: [
            {
                name: 'title.scss',
                transform: true,
            },
            {
                name: 'title.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'title.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
    {
        tag: 'ion-toolbar',
        srcDir: 'src/components/toolbar',
        include: '.scss',
        specifics: [
            {
                name: 'toolbar.scss',
                transform: true,
            },
            {
                name: 'toolbar.md.scss',
                transform: true,
                import: true,
                suffix: '.md'
            },
            {
                name: 'toolbar.ios.scss',
                transform: true,
                import: true,
                suffix: '.ios'
            }
        ]
    },
]

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
    const imports = [];
    for (const component of components) {
        const componentPath = path.resolve(component.root ? component.root : srcPath, component.srcDir);
        const fileNames = fs.readdirSync(componentPath);
        for (let fileName of fileNames) {
            // 跳过无需处理的文件类型
            if (path.extname(fileName) !== component.include) {
                continue;
            }
            const specific = component.specifics.find(x => x.name === fileName);
            const srcFile = path.resolve(componentPath, fileName);
            let distFile = path.resolve(component.srcDir, fileName);
            if (specific) {
                if (specific.extname) {
                    fileName = fileName.replace(path.extname(fileName), specific.extname);
                    distFile = distFile.replace(path.extname(distFile), specific.extname);
                }

                if (specific.import) {
                    imports.push(`${component.srcDir.replace('src', '..')}/${fileName}`);
                }

                if (specific.transform) {
                    const name = specific.suffix ? `${component.tag}${specific.suffix}` : component.tag;
                    const file = fs.readFileSync(srcFile, 'utf8');
                    const content = transform(name, file);
                    console.log(distFile);
                    fs.writeFileSync(distFile, content);
                } else {
                    fs.copyFileSync(srcFile, distFile);
                }
            } else {
                fs.copyFileSync(srcFile, distFile);
            }
        }
    }

    const context = {
        path: 'src/css/components.scss',
        imports: imports
    }
    execSync(`"./node_modules/.bin/hygen" generator component component --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

function generateIcons() {
    const context = {
        path: 'src/components/icon/IconResource.cs',
        svgRoot: '../../ionic/ionicons/src/svg'
    }
    execSync(`"./node_modules/.bin/hygen" generator icon icon --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

function generate() {
    generateCss();
    generateIcons();
}

generate();